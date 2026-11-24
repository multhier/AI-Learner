using System.Security.Claims;
using Kivilcim.Data;
using Kivilcim.Models;
using Kivilcim.Services;
using Microsoft.AspNetCore.Mvc;

namespace Kivilcim.Controllers;

public class ExploreController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly PostQueryService _posts;

    public ExploreController(ApplicationDbContext db, PostQueryService posts)
    {
        _db = db;
        _posts = posts;
    }

    private string? Me => User.FindFirstValue(ClaimTypes.NameIdentifier);

    [HttpGet("/explore")]
    public async Task<IActionResult> Index()
    {
        return View(new ExploreViewModel
        {
            Heading = "Keşfet",
            Results = new PostListViewModel
            {
                Posts = await _posts.GetPopularAsync(Me),
                EmptyMessage = "Son 30 günde popüler gönderi yok."
            },
            TrendingTags = await _posts.GetTrendingTagsAsync()
        });
    }

    [HttpGet("/tagged/{tag}")]
    public async Task<IActionResult> Tagged(string tag, int? before, bool partial = false)
    {
        var name = TagParser.Normalize(tag);
        var query = _db.Posts.Where(p => p.PostTags.Any(pt => pt.Tag.Name == name));
        var list = await BuildListAsync(query, before, b => Url.Action(nameof(Tagged), new { tag = name, before = b }),
            $"#{name} etiketiyle henüz gönderi yok.");

        if (partial) return PartialView("_PostList", list);

        return View("Index", new ExploreViewModel
        {
            Heading = $"#{name}",
            Tag = name,
            Results = list,
            TrendingTags = await _posts.GetTrendingTagsAsync()
        });
    }

    [HttpGet("/search")]
    public async Task<IActionResult> Search(string? q, int? before, bool partial = false)
    {
        var term = (q ?? "").Trim();
        if (term.Length == 0)
        {
            return RedirectToAction(nameof(Index));
        }
        if (term.Length > 100) term = term[..100];

        var tagName = TagParser.Normalize(term);
        var lowered = term.ToLower();
        var query = _db.Posts.Where(p =>
            (p.Title != null && p.Title.ToLower().Contains(lowered)) ||
            (p.Body != null && p.Body.ToLower().Contains(lowered)) ||
            p.PostTags.Any(pt => pt.Tag.Name == tagName));

        var list = await BuildListAsync(query, before, b => Url.Action(nameof(Search), new { q = term, before = b }),
            $"\"{term}\" için sonuç bulunamadı.");

        if (partial) return PartialView("_PostList", list);

        return View("Index", new ExploreViewModel
        {
            Heading = $"\"{term}\" için sonuçlar",
            Query = term,
            Results = list,
            TrendingTags = await _posts.GetTrendingTagsAsync()
        });
    }

    private async Task<PostListViewModel> BuildListAsync(
        IQueryable<Post> query, int? before, Func<int, string?> nextUrl, string emptyMessage)
    {
        var list = new PostListViewModel
        {
            Posts = await _posts.GetPageAsync(query, Me, before),
            EmptyMessage = emptyMessage
        };
        if (list.Posts.Count == PostQueryService.PageSize)
        {
            list.NextPageUrl = nextUrl(list.Posts[^1].Id);
        }
        return list;
    }
}
