using Kivilcim.Bll.Abstractions;
using Kivilcim.Bll.Dtos;
using Kivilcim.Core.Results;
using Kivilcim.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kivilcim.Web.Controllers;

[Authorize]
[Route("settings")]
public class SettingsController : KivilcimController
{
    private readonly IProfileService _profiles;

    public SettingsController(IProfileService profiles) => _profiles = profiles;

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var profile = await _profiles.GetProfileAsync(CurrentUserId!);
        if (profile == null) return NotFound();
        return View(ToForm(profile));
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Index(ProfileSettingsViewModel model)
    {
        var avatar = model.Avatar is { Length: > 0 } file ? ToMedia(file) : null;
        var result = await _profiles.UpdateProfileAsync(CurrentUserId!,
            new ProfileUpdate(model.BlogName, model.DisplayName, model.Bio), avatar);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Field, error.Message);
            }
            var current = await _profiles.GetProfileAsync(CurrentUserId!);
            return View(ToForm(current, model));
        }

        TempData["Message"] = "Profilin güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("hidden-tags")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddHiddenTag(string? tag)
    {
        var result = await _profiles.AddHiddenTagAsync(CurrentUserId!, tag);
        if (result.Succeeded)
        {
            TempData["Message"] = $"#{tag} artık akışında görünmeyecek.";
        }
        return RedirectToAction(nameof(Index), null, "gizli-etiketler");
    }

    [HttpPost("hidden-tags/remove")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveHiddenTag(string tag)
    {
        await _profiles.RemoveHiddenTagAsync(CurrentUserId!, tag);
        return RedirectToAction(nameof(Index), null, "gizli-etiketler");
    }

    private static ProfileSettingsViewModel ToForm(ProfileDto? profile, ProfileSettingsViewModel? posted = null) => new()
    {
        BlogName = posted?.BlogName ?? profile?.BlogName ?? "",
        DisplayName = posted?.DisplayName ?? profile?.DisplayName ?? "",
        Bio = posted?.Bio ?? profile?.Bio,
        CurrentAvatarPath = profile?.AvatarPath,
        HiddenTags = profile?.HiddenTags ?? new List<string>()
    };
}
