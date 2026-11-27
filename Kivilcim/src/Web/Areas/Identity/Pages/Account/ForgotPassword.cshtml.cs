using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using Kivilcim.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace Kivilcim.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class ForgotPasswordModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender _emailSender;

    public ForgotPasswordModel(UserManager<ApplicationUser> userManager, IEmailSender emailSender)
    {
        _userManager = userManager;
        _emailSender = emailSender;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "E-posta zorunlu.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
        [Display(Name = "E-posta")]
        public string Email { get; set; } = "";
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.FindByEmailAsync(Input.Email);
        // Hesap var mı yok mu belli etmemek için her durumda aynı sayfaya gidilir.
        if (user != null)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var link = Url.Page("/Account/ResetPassword", pageHandler: null,
                values: new { area = "Identity", code, email = user.Email }, protocol: Request.Scheme)!;

            var safeLink = HtmlEncoder.Default.Encode(link);
            await _emailSender.SendEmailAsync(user.Email!, "Kıvılcım şifre sıfırlama",
                $"""
                <div style="font-family:Segoe UI,Arial,sans-serif;font-size:15px;color:#1b2433">
                  <h2 style="color:#ff6a3d">✦ Kıvılcım</h2>
                  <p>Merhaba {HtmlEncoder.Default.Encode(user.DisplayName)},</p>
                  <p>Şifreni sıfırlamak için aşağıdaki düğmeye tıkla. Bağlantı bir gün geçerlidir.</p>
                  <p><a href="{safeLink}" style="background:#ff6a3d;color:#fff;padding:10px 18px;border-radius:6px;text-decoration:none">Şifremi sıfırla</a></p>
                  <p style="color:#6b7686;font-size:13px">Bu isteği sen yapmadıysan bu e-postayı yok sayabilirsin.</p>
                </div>
                """);
        }

        return RedirectToPage("./ForgotPasswordConfirmation");
    }
}
