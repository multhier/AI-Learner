using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sozluk.Web.Data;
using Sozluk.Web.Models;

namespace Sozluk.Web.Controllers;

public class AccountController(SozlukDbContext db) : Controller
{
    private static readonly PasswordHasher<User> Hasher = new();

    [HttpGet("/giris")]
    public IActionResult Login(string? returnUrl) => View(new LoginInput { ReturnUrl = returnUrl });

    [HttpPost("/giris")]
    public async Task<IActionResult> Login(LoginInput input)
    {
        if (!ModelState.IsValid) return View(input);

        var name = input.UserName.Trim();
        var user = await db.Users.FirstOrDefaultAsync(u => u.UserName == name || u.Email == name);
        if (user == null || Hasher.VerifyHashedPassword(user, user.PasswordHash, input.Password) == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError("", "kullanıcı adı ya da şifre hatalı");
            return View(input);
        }
        if (user.IsBanned)
        {
            ModelState.AddModelError("", "hesabın askıya alınmış");
            return View(input);
        }

        await SignInAsync(user);
        return LocalRedirect(Url.IsLocalUrl(input.ReturnUrl) ? input.ReturnUrl! : "/");
    }

    [HttpGet("/kayit")]
    public IActionResult Register() => View(new RegisterInput());

    [HttpPost("/kayit")]
    public async Task<IActionResult> Register(RegisterInput input)
    {
        if (!ModelState.IsValid) return View(input);

        var nick = input.UserName.Trim().ToLower(new System.Globalization.CultureInfo("tr-TR"));
        var email = input.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.UserName == nick))
            ModelState.AddModelError(nameof(input.UserName), "bu nick alınmış");
        if (await db.Users.AnyAsync(u => u.Email == email))
            ModelState.AddModelError(nameof(input.Email), "bu e-posta kayıtlı");
        if (!ModelState.IsValid) return View(input);

        var user = new User { UserName = nick, Email = email, Role = UserRole.Caylak };
        user.PasswordHash = Hasher.HashPassword(user, input.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        await SignInAsync(user);
        TempData["Info"] = "hoş geldin çaylak. entry'lerin moderasyon onayıyla yazarlığa terfi edeceksin.";
        return Redirect("/");
    }

    [Authorize, HttpPost("/cikis")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/");
    }

    private Task SignInAsync(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Role, user.Role.ToString()),
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        return HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = true });
    }
}
