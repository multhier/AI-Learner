using System.Diagnostics;
using System.Security.Claims;
using Kivilcim.Data;
using Kivilcim.Models;
using Kivilcim.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kivilcim.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly PostQueryService _posts;

    public HomeController(ApplicationDbContext db, PostQueryService posts)
    {
        _db = db;
        _posts = posts;
    }

    /// <summary>Girişliyse takip edilenlerin akışı (dashboard), değilse popüler gönderiler.</summary>
    public async Task<IActionResult> Index(int? before, bool partial = false)
    {
        var me = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (me == null)
        {
            var popular = await _posts.GetPopularAsync(null);
            return View(new HomeViewModel
            {
                IsDashboard = false,
                Feed = new PostListViewModel { Posts = popular, EmptyMessage = "Henüz paylaşılan bir şey yok." },
                TrendingTags = await _posts.GetTrendingTagsAsync()
            });
        }

        var followees = _db.Follows.Where(f => f.FollowerId == me).Select(f => f.FolloweeId);
        var query = _db.Posts.Where(p => p.AuthorId == me || followees.Contains(p.AuthorId));
        var feed = new PostListViewModel
        {
            Posts = await _posts.GetPageAsync(query, me, before),
            EmptyMessage = "Akışın boş. Keşfet'ten birkaç blog takip etmeye ne dersin?"
        };
        if (feed.Posts.Count == PostQueryService.PageSize)
        {
            feed.NextPageUrl = Url.Action(nameof(Index), new { before = feed.Posts[^1].Id });
        }

        if (partial)
        {
            return PartialView("_PostList", feed);
        }

        var suggestions = await _db.Users
            .Where(u => u.Id != me && !_db.Follows.Any(f => f.FollowerId == me && f.FolloweeId == u.Id))
            .OrderByDescending(u => u.Posts.Count)
            .Take(5)
            .Select(u => new BlogSuggestion(u.BlogName!, u.DisplayName, u.AvatarPath, u.Bio))
            .ToListAsync();

        return View(new HomeViewModel
        {
            IsDashboard = true,
            Feed = feed,
            TrendingTags = await _posts.GetTrendingTagsAsync(),
            Suggestions = suggestions
        });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
