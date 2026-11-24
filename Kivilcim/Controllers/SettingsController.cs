using System.Security.Claims;
using Kivilcim.Data;
using Kivilcim.Models;
using Kivilcim.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kivilcim.Controllers;

[Authorize]
[Route("settings")]
public class SettingsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ImageStore _images;

    public SettingsController(ApplicationDbContext db, ImageStore images)
    {
        _db = db;
        _images = images;
    }

    private string Me => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var user = await _db.Users.FirstAsync(u => u.Id == Me);
        return View(await FillAsync(new ProfileSettingsViewModel
        {
            BlogName = user.BlogName ?? "",
            DisplayName = user.DisplayName,
            Bio = user.Bio
        }, user));
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Index(ProfileSettingsViewModel model)
    {
        var user = await _db.Users.FirstAsync(u => u.Id == Me);
        var blogName = (model.BlogName ?? "").Trim().ToLowerInvariant();

        if (!BlogNames.ValidPattern.IsMatch(blogName))
        {
            ModelState.AddModelError(nameof(model.BlogName),
                "3-32 karakter; küçük harf, rakam ve tire kullanın. Tire ile başlayıp bitemez.");
        }
        else if (await _db.Users.AnyAsync(u => u.BlogName == blogName && u.Id != user.Id))
        {
            ModelState.AddModelError(nameof(model.BlogName), "Bu blog adı alınmış.");
        }

        string? newAvatar = null;
        if (ModelState.IsValid && model.Avatar != null)
        {
            var (path, error) = await _images.SaveAsync(model.Avatar, "avatars");
            if (error != null) ModelState.AddModelError(nameof(model.Avatar), error);
            newAvatar = path;
        }

        if (!ModelState.IsValid)
        {
            return View(await FillAsync(model, user));
        }

        user.BlogName = blogName;
        user.DisplayName = model.DisplayName.Trim();
        user.Bio = string.IsNullOrWhiteSpace(model.Bio) ? null : model.Bio.Trim();
        if (newAvatar != null)
        {
            _images.Delete(user.AvatarPath);
            user.AvatarPath = newAvatar;
        }
        await _db.SaveChangesAsync();

        TempData["Message"] = "Profilin güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("hidden-tags")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddHiddenTag(string? tag)
    {
        var name = TagParser.Normalize(tag ?? "");
        if (name.Length > 0 && !await _db.HiddenTags.AnyAsync(h => h.UserId == Me && h.TagName == name))
        {
            _db.HiddenTags.Add(new HiddenTag { UserId = Me, TagName = name });
            await _db.SaveChangesAsync();
            TempData["Message"] = $"#{name} artık akışında görünmeyecek.";
        }
        return RedirectToAction(nameof(Index), null, "gizli-etiketler");
    }

    [HttpPost("hidden-tags/remove")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveHiddenTag(string tag)
    {
        var item = await _db.HiddenTags.FindAsync(Me, tag);
        if (item != null)
        {
            _db.HiddenTags.Remove(item);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index), null, "gizli-etiketler");
    }

    private async Task<ProfileSettingsViewModel> FillAsync(ProfileSettingsViewModel model, ApplicationUser user)
    {
        model.CurrentAvatarPath = user.AvatarPath;
        model.HiddenTags = await _db.HiddenTags.Where(h => h.UserId == user.Id)
            .OrderBy(h => h.TagName).Select(h => h.TagName).ToListAsync();
        return model;
    }
}
