using System.ComponentModel.DataAnnotations;
using Kivilcim.Models;
using Kivilcim.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kivilcim.Areas.Identity.Pages.Account.Manage;

public class DeletePersonalDataModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ImageStore _files;
    private readonly Kivilcim.Data.ApplicationDbContext _db;

    public DeletePersonalDataModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager,
        ImageStore files, Kivilcim.Data.ApplicationDbContext db)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _files = files;
        _db = db;
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

        // Diskteki dosyaları silebilmek için yolları önceden topla.
        var files = _db.Posts.Where(p => p.AuthorId == user.Id)
            .Select(p => new { p.ImagePath, p.VideoPath }).ToList();

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Hesap silinirken bir hata oluştu.");
            return Page();
        }

        foreach (var f in files)
        {
            _files.Delete(f.ImagePath);
            _files.Delete(f.VideoPath);
        }
        _files.Delete(user.AvatarPath);

        await _signInManager.SignOutAsync();
        TempData["Message"] = "Hesabın ve tüm gönderilerin silindi.";
        return Redirect("~/");
    }
}
