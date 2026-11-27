using Kivilcim.Bll.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kivilcim.Web.Controllers;

public class BlogController : KivilcimController
{
    private readonly IBlogService _blogs;
    private readonly IPostQueryService _posts;

    public BlogController(IBlogService blogs, IPostQueryService posts)
    {
        _blogs = blogs;
        _posts = posts;
    }

    [HttpGet("/blog/{blogName}")]
    public async Task<IActionResult> Index(string blogName, int? before, bool partial = false)
    {
        var blog = await _blogs.GetBlogAsync(blogName, CurrentUserId, before);
        if (blog == null)
        {
            return NotFound();
        }

        blog.Posts.EmptyMessage = "Bu blogda henüz gönderi yok.";
        blog.Posts.NextPageUrl = NextPageUrl(blog.Posts.Posts, _posts.PageSize, nameof(Index), null,
            new { blogName, before = blog.Posts.Posts.LastOrDefault()?.Id, partial = true });

        if (partial) return PartialView("_PostList", blog.Posts);
        return View(blog);
    }

    [Authorize]
    [HttpPost("/blog/{blogName}/follow")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Follow(string blogName, string? returnUrl)
    {
        var result = await _blogs.ToggleFollowAsync(CurrentUserId!, blogName);
        if (result == null) return NotFound();

        return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction(nameof(Index), new { blogName });
    }
}
