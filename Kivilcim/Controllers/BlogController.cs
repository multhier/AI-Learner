using System.Security.Claims;
using Kivilcim.Data;
using Kivilcim.Models;
using Kivilcim.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kivilcim.Controllers;

public class BlogController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly PostQueryService _posts;

    public BlogController(ApplicationDbContext db, PostQueryService posts)
    {
        _db = db;
        _posts = posts;
    }

    private string? Me => User.FindFirstValue(ClaimTypes.NameIdentifier);

    [HttpGet("/blog/{blogName}")]
    public async Task<IActionResult> Index(string blogName, int? before, bool partial = false)
    {
        var me = Me;
        var blog = await _db.Users
            .Where(u => u.BlogName == blogName)
            .Select(u => new
            {
                u.Id, u.BlogName, u.DisplayName, u.Bio, u.AvatarPath, u.CreatedAt,
                PostCount = u.Posts.Count,
                FollowerCount = u.Followers.Count,
                FollowingCount = u.Following.Count,
                IsFollowing = me != null && u.Followers.Any(f => f.FollowerId == me)
            })
            .FirstOrDefaultAsync();
        if (blog == null) return NotFound();

        // Blog sayfasında gizli etiket filtresi uygulanmaz: ziyaretçi o blogu bilerek açtı.
        var list = new PostListViewModel
        {
            Posts = await _posts.GetPageAsync(_db.Posts.Where(p => p.AuthorId == blog.Id), me, before, applyHiddenTags: false),
            EmptyMessage = "Bu blogda henüz gönderi yok."
        };
        if (list.Posts.Count == PostQueryService.PageSize)
        {
            list.NextPageUrl = Url.Action(nameof(Index), new { blogName, before = list.Posts[^1].Id });
        }

        if (partial) return PartialView("_PostList", list);

        return View(new BlogViewModel
        {
            BlogName = blog.BlogName!,
            DisplayName = blog.DisplayName,
            Bio = blog.Bio,
            AvatarPath = blog.AvatarPath,
            JoinedAt = blog.CreatedAt,
            PostCount = blog.PostCount,
            FollowerCount = blog.FollowerCount,
            FollowingCount = blog.FollowingCount,
            IsMe = blog.Id == me,
            IsFollowing = blog.IsFollowing,
            Posts = list
        });
    }

    [Authorize]
    [HttpPost("/blog/{blogName}/follow")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Follow(string blogName, string? returnUrl)
    {
        var me = Me!;
        var targetId = await _db.Users.Where(u => u.BlogName == blogName).Select(u => u.Id).FirstOrDefaultAsync();
        if (targetId == null) return NotFound();

        if (targetId != me)
        {
            var existing = await _db.Follows.FindAsync(me, targetId);
            if (existing == null)
            {
                _db.Follows.Add(new Follow { FollowerId = me, FolloweeId = targetId });
            }
            else
            {
                _db.Follows.Remove(existing);
            }
            await _db.SaveChangesAsync();
        }

        return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction(nameof(Index), new { blogName });
    }
}
