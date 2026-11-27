using System.ComponentModel.DataAnnotations;
using Kivilcim.Bll.Abstractions;
using Kivilcim.Core.Abstractions;
using Kivilcim.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kivilcim.Areas.Identity.Pages.Account.Manage;

public class DeletePersonalDataModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IProfileService _profiles;
    private readonly IFileStorage _files;

    public DeletePersonalDataModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager,
        IProfileService profiles, IFileStorage files)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _profiles = profiles;
        _files = files;
    }

    [BindProperty]
    [Required(ErrorMessage = "Onay için şifreni gir.")]
    [DataType(DataType.Password)]
    [Display(Name = "Şifre")]
    public string Password { get; set; } = "";

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user == null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();
        if (!ModelState.IsValid) return Page();

        if (!await _userManager.CheckPasswordAsync(user, Password))
        {
            ModelState.AddModelError(nameof(Password), "Şifre yanlış.");
            return Page();
        }

        // Dosyaların yolları silme işleminden önce alınır; silme başarılıysa diskteki dosyalar da silinir.
        var files = await _profiles.CollectAccountFilesAsync(user.Id);

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Hesap silinirken bir hata oluştu.");
            return Page();
        }

        foreach (var path in files)
        {
            _files.Delete(path);
        }

        await _signInManager.SignOutAsync();
        TempData["Message"] = "Hesabın ve tüm gönderilerin silindi.";
        return Redirect("~/");
    }
}
