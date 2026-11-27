using Kivilcim.Bll.Abstractions;
using Kivilcim.Bll.Dtos;
using Kivilcim.Core.Results;
using Kivilcim.Model;
using Kivilcim.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kivilcim.Web.Controllers;

public class PostsController : KivilcimController
{
    private readonly IPostService _posts;
    private readonly IPostQueryService _queries;
    private readonly IProfileService _profiles;

    public PostsController(IPostService posts, IPostQueryService queries, IProfileService profiles)
    {
        _posts = posts;
        _queries = queries;
        _profiles = profiles;
    }

    [HttpGet("/post/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var details = await _queries.GetDetailsAsync(id, CurrentUserId);
        return details == null ? NotFound() : View(details);
    }

    [Authorize]
    [HttpGet("/new/{type?}")]
    public IActionResult Create(PostType type = PostType.Text)
    {
        return View(new CreatePostViewModel { Type = type });
    }

    [Authorize]
    [HttpPost("/new/{type?}")]
    [ValidateAntiForgeryToken]
    // 30 görsel x 5 MB ya da 50 MB video + form alanları
    [RequestSizeLimit(160 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 160 * 1024 * 1024, ValueCountLimit = 200)]
    public async Task<IActionResult> Create(CreatePostViewModel model)
    {
        var command = new CreatePostCommand(
            model.Type,
            model.Title,
            model.Body,
            model.LinkUrl,
            model.Tags,
            model.Images.Where(f => f.Length > 0).Select(ToMedia).ToList(),
            model.ImageUrls,
            model.Video is { Length: > 0 } video ? ToMedia(video) : null);

        var result = await _posts.CreateAsync(CurrentUserId!, command);
        if (!result.Succeeded)
        {
            AddErrors(result);
            return View(model);
        }

        TempData["Message"] = "Gönderin paylaşıldı.";
        return RedirectToAction(nameof(Details), new { id = result.Value });
    }

    [Authorize]
    [HttpGet("/post/{id:int}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var dto = await _queries.GetForEditAsync(id, CurrentUserId!);
        if (dto == null) return NotFound();

        return View(new EditPostViewModel
        {
            Id = dto.Id,
            Type = dto.Type,
            Title = dto.Title,
            Body = dto.Body,
            LinkUrl = dto.LinkUrl,
            Tags = dto.Tags,
            Images = dto.Images
        });
    }

    [Authorize]
    [HttpPost("/post/{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditPostViewModel model)
    {
        var result = await _posts.UpdateAsync(CurrentUserId!, id,
            new EditPostCommand(model.Title, model.Body, model.LinkUrl, model.Tags));

        if (!result.Succeeded)
        {
            if (result.Errors.Any(e => e.Field == "NotFound")) return NotFound();
            if (result.Errors.Any(e => e.Field == "Forbidden")) return Forbid();
            AddErrors(result);
            model.Id = id;
            model.Images = (await _queries.GetForEditAsync(id, CurrentUserId!))?.Images ?? new();
            return View(model);
        }

        TempData["Message"] = "Gönderi güncellendi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Like(int id, string? returnUrl)
    {
        var result = await _posts.ToggleLikeAsync(CurrentUserId!, id);
        if (result == null) return NotFound();

        if (IsFetch)
        {
            return Json(new { liked = result.Liked, count = result.Count });
        }
        return BackTo(returnUrl, id);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reblog(int id, string? comment, string? tags, string? returnUrl)
    {
        var result = await _posts.ReblogAsync(CurrentUserId!, id, comment, tags);
        if (!result.Succeeded) return NotFound();

        TempData["Message"] = "Yeniden paylaşıldı.";
        return BackTo(returnUrl, result.Value);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(int id, string? body)
    {
        var result = await _posts.ReplyAsync(CurrentUserId!, id, body);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.FirstError;
        }
        return Redirect(Url.Action(nameof(Details), new { id }) + "#yanitlar");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _posts.DeleteAsync(CurrentUserId!, id);
        if (result.Errors.Any(e => e.Field == "NotFound")) return NotFound();
        if (result.Errors.Any(e => e.Field == "Forbidden")) return Forbid();

        TempData["Message"] = "Gönderi silindi.";
        var profile = await _profiles.GetProfileAsync(CurrentUserId!);
        return RedirectToAction("Index", "Blog", new { blogName = profile?.BlogName });
    }

    private void AddErrors(Result result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(error.Field, error.Message);
        }
    }
}
