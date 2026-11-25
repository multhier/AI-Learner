using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebKoy.Web.Data;
using WebKoy.Web.Models;
using WebKoy.Web.Services;

namespace WebKoy.Web.Controllers;

[Route("account/[action]")]
public class AccountController(
    UserManager<ApplicationUser> users,
    SignInManager<ApplicationUser> signIn,
    AppDbContext db,
    SiteProvisioner provisioner,
    IFileStorage storage,
    IEmailSender email,
    IOptions<PlatformOptions> options,
    ILogger<AccountController> logger) : AppController
{
    private readonly PlatformOptions _opt = options.Value;

    [HttpGet]
    public IActionResult Register() => User.Identity?.IsAuthenticated == true ? RedirectToAction("Index", "Dashboard") : View(new RegisterViewModel());

    [HttpPost, EnableRateLimiting("auth")]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        model.SiteName = SiteNameRules.Normalize(model.SiteName);
        if (SiteNameRules.Validate(model.SiteName) is { } nameError)
            ModelState.AddModelError(nameof(model.SiteName), nameError);
        else if (await db.Sites.AnyAsync(s => s.Name == model.SiteName) || await users.FindByNameAsync(model.SiteName) is not null)
            ModelState.AddModelError(nameof(model.SiteName), "Bu site adı alınmış.");

        if (!ModelState.IsValid) return View(model);

        var user = new ApplicationUser { UserName = model.SiteName, Email = model.Email.Trim() };
        var strategy = db.Database.CreateExecutionStrategy();
        IdentityResult? identityResult = null;

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            identityResult = await users.CreateAsync(user, model.Password);
            if (!identityResult.Succeeded) return;

            var site = new Site { OwnerId = user.Id, Name = model.SiteName };
            db.Sites.Add(site);
            await db.SaveChangesAsync();
            await provisioner.ProvisionAsync(site);
            site.UpdatedAt = site.CreatedAt;
            await db.SaveChangesAsync();
            await tx.CommitAsync();
        });

        if (!identityResult!.Succeeded)
        {
            foreach (var e in identityResult.Errors)
                ModelState.AddModelError("", TranslateIdentityError(e));
            return View(model);
        }

        logger.LogInformation("Yeni site açıldı: {Site}", model.SiteName);
        if (_opt.AdminEmails.Contains(user.Email, StringComparer.OrdinalIgnoreCase))
            await users.AddToRoleAsync(user, DbInitializer.AdminRole);
        await SendConfirmationAsync(user);

        if (_opt.RequireConfirmedEmail)
        {
            Flash("Hesabınız oluşturuldu. Giriş yapmadan önce e-postanıza gelen bağlantıyla adresinizi doğrulayın.");
            return RedirectToAction(nameof(Login));
        }

        await signIn.SignInAsync(user, isPersistent: false);
        Flash($"Siteniz hazır: {_opt.SiteUrl(model.SiteName)}");
        return RedirectToAction("Index", "Dashboard");
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost, EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var login = model.Login.Trim();
        var user = login.Contains('@') ? await users.FindByEmailAsync(login) : await users.FindByNameAsync(login.ToLowerInvariant());
        if (user is null)
        {
            ModelState.AddModelError("", "Giriş bilgileri hatalı.");
            return View(model);
        }

        var result = await signIn.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
            return Url.IsLocalUrl(model.ReturnUrl) ? Redirect(model.ReturnUrl!) : RedirectToAction("Index", "Dashboard");

        ModelState.AddModelError("", result switch
        {
            { IsLockedOut: true } => "Çok fazla hatalı deneme. Lütfen 15 dakika sonra tekrar deneyin.",
            { IsNotAllowed: true } => "E-posta adresinizi doğrulamanız gerekiyor.",
            _ => "Giriş bilgileri hatalı.",
        });
        return View(model);
    }

    [HttpPost, Authorize]
    public async Task<IActionResult> Logout()
    {
        await signIn.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(string userId, string code)
    {
        var user = await users.FindByIdAsync(userId);
        if (user is null) return NotFound();
        var token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
        var result = await users.ConfirmEmailAsync(user, token);
        Flash(result.Succeeded ? "E-posta adresiniz doğrulandı, giriş yapabilirsiniz." : "Doğrulama bağlantısı geçersiz veya süresi dolmuş.", !result.Succeeded);
        return RedirectToAction(nameof(Login));
    }

    [HttpPost, Authorize]
    public async Task<IActionResult> Delete(string confirmName)
    {
        var user = await users.GetUserAsync(User);
        var site = await db.Sites.FirstOrDefaultAsync(s => s.OwnerId == UserId);
        if (user is null || site is null) return NotFound();
        if (!string.Equals(confirmName?.Trim(), site.Name, StringComparison.OrdinalIgnoreCase))
        {
            Flash("Silmek için site adınızı doğru yazmalısınız.", error: true);
            return RedirectToAction("Settings", "Dashboard");
        }

        // Kullanıcıya bağlı NoAction ilişkileri elle temizlenir.
        await db.Follows.Where(f => f.UserId == user.Id).ExecuteDeleteAsync();
        await db.Likes.Where(l => l.UserId == user.Id).ExecuteDeleteAsync();
        await db.Comments.Where(c => c.AuthorId == user.Id).ExecuteDeleteAsync();
        await db.Reports.Where(r => r.ReporterId == user.Id).ExecuteUpdateAsync(r => r.SetProperty(x => x.ReporterId, (string?)null));

        var siteId = site.Id;
        await signIn.SignOutAsync();
        await users.DeleteAsync(user); // Site ve dosya kayıtları cascade ile silinir.
        storage.DeleteSite(siteId);

        Flash("Hesabınız ve siteniz kalıcı olarak silindi.");
        return RedirectToAction("Index", "Home");
    }

    private async Task SendConfirmationAsync(ApplicationUser user)
    {
        var token = await users.GenerateEmailConfirmationTokenAsync(user);
        var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var link = Url.Action(nameof(ConfirmEmail), "Account", new { userId = user.Id, code }, Request.Scheme)!;
        await email.SendEmailAsync(user.Email!, "E-posta adresinizi doğrulayın",
            $"Merhaba {HtmlEncoder.Default.Encode(user.UserName!)},<br>Hesabınızı doğrulamak için <a href=\"{HtmlEncoder.Default.Encode(link)}\">buraya tıklayın</a>.");
    }

    private static string TranslateIdentityError(IdentityError e) => e.Code switch
    {
        "DuplicateEmail" => "Bu e-posta adresi zaten kayıtlı.",
        "DuplicateUserName" => "Bu site adı alınmış.",
        "InvalidEmail" => "Geçersiz e-posta adresi.",
        "PasswordTooShort" => "Şifre en az 8 karakter olmalı.",
        "PasswordRequiresDigit" => "Şifre en az bir rakam içermeli.",
        "PasswordRequiresLower" => "Şifre en az bir küçük harf içermeli.",
        _ => e.Description,
    };
}
