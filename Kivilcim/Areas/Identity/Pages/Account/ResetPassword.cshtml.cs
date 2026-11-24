using System.ComponentModel.DataAnnotations;
using System.Text;
using Kivilcim.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace Kivilcim.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class ResetPasswordModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ResetPasswordModel(UserManager<ApplicationUser> userManager) => _userManager = userManager;

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        public string Email { get; set; } = "";

        [Required]
        public string Code { get; set; } = "";

        [Required(ErrorMessage = "Yeni şifre zorunlu.")]
        [StringLength(100, ErrorMessage = "Şifre en az {2}, en fazla {1} karakter olmalı.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Yeni şifre")]
        public string Password { get; set; } = "";

        [DataType(DataType.Password)]
        [Display(Name = "Yeni şifre (tekrar)")]
        [Compare(nameof(Password), ErrorMessage = "Şifreler eşleşmiyor.")]
        public string ConfirmPassword { get; set; } = "";
    }

    public IActionResult OnGet(string? code = null, string? email = null)
    {
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(email))
        {
            TempData["Error"] = "Şifre sıfırlama bağlantısı eksik ya da hatalı.";
            return RedirectToPage("./ForgotPassword");
        }
        Input = new InputModel { Code = code, Email = email };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.FindByEmailAsync(Input.Email);
        if (user == null)
        {
            // Hesabın varlığını belli etme.
            return RedirectToPage("./ResetPasswordConfirmation");
        }

        string token;
        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(Input.Code));
        }
        catch (FormatException)
        {
            ModelState.AddModelError(string.Empty, "Bağlantı geçersiz. Lütfen yeni bir sıfırlama isteği gönder.");
            return Page();
        }

        var result = await _userManager.ResetPasswordAsync(user, token, Input.Password);
        if (result.Succeeded)
        {
            return RedirectToPage("./ResetPasswordConfirmation");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Code == "InvalidToken"
                ? "Bağlantının süresi dolmuş ya da daha önce kullanılmış. Lütfen yeni bir sıfırlama isteği gönder."
                : error.Description);
        }
        return Page();
    }
}
