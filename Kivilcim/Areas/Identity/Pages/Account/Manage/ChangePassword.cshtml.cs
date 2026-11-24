using System.ComponentModel.DataAnnotations;
using Kivilcim.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kivilcim.Areas.Identity.Pages.Account.Manage;

public class ChangePasswordModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public ChangePasswordModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Mevcut şifre zorunlu.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mevcut şifre")]
        public string OldPassword { get; set; } = "";

        [Required(ErrorMessage = "Yeni şifre zorunlu.")]
        [StringLength(100, ErrorMessage = "Şifre en az {2}, en fazla {1} karakter olmalı.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Yeni şifre")]
        public string NewPassword { get; set; } = "";

        [DataType(DataType.Password)]
        [Display(Name = "Yeni şifre (tekrar)")]
        [Compare(nameof(NewPassword), ErrorMessage = "Şifreler eşleşmiyor.")]
        public string ConfirmPassword { get; set; } = "";
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user == null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        var result = await _userManager.ChangePasswordAsync(user, Input.OldPassword, Input.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return Page();
        }

        await _signInManager.RefreshSignInAsync(user);
        StatusMessage = "Şifren değiştirildi.";
        return RedirectToPage();
    }
}
