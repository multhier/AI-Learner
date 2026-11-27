using Kivilcim.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Kivilcim.Areas.Identity.Pages.Account.Manage;

public class IndexModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(UserManager<ApplicationUser> userManager) => _userManager = userManager;

    public string Email { get; set; } = "";
    public string BlogName { get; set; } = "";
    public DateTime CreatedAt { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();
        Email = user.Email ?? "";
        BlogName = user.BlogName ?? "";
        CreatedAt = user.CreatedAt;
        return Page();
    }
}
