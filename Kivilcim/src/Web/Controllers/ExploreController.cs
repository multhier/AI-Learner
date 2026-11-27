using Kivilcim.Bll.Abstractions;
using Kivilcim.Bll.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Kivilcim.Web.Controllers;

public class ExploreController : KivilcimController
{
    private readonly IPostQueryService _posts;

    public ExploreController(IPostQueryService posts) => _posts = posts;

    [HttpGet("/explore")]
    public async Task<IActionResult> Index()
    {
        return View(new ExploreViewModel
        {
            Heading = "Keşfet",
            Results = new PostListViewModel
            {
                Posts = await _posts.PopularAsync(CurrentUserId),
                EmptyMessage = "Son 30 günde popüler gönderi yok."
            },
            TrendingTags = await _posts.TrendingTagsAsync()
        });
    }

    [HttpGet("/tagged/{tag}")]
    public async Task<IActionResult> Tagged(string tag, int? before, bool partial = false)
    {
        var cards = await _posts.TaggedPageAsync(tag, CurrentUserId, before);
        var list = new PostListViewModel
        {
            Posts = cards,
            EmptyMessage = $"#{tag} etiketiyle henüz gönderi yok.",
            NextPageUrl = NextPageUrl(cards, _posts.PageSize, nameof(Tagged), null, new { tag, before = cards.LastOrDefault()?.Id, partial = true })
        };

        if (partial) return PartialView("_PostList", list);

        return View("Index", new ExploreViewModel
        {
            Heading = $"#{tag}",
            Tag = tag,
            Results = list,
            TrendingTags = await _posts.TrendingTagsAsync()
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

        var cards = await _posts.SearchPageAsync(term, CurrentUserId, before);
        var list = new PostListViewModel
        {
            Posts = cards,
            EmptyMessage = $"\"{term}\" için sonuç bulunamadı.",
            NextPageUrl = NextPageUrl(cards, _posts.PageSize, nameof(Search), null, new { q = term, before = cards.LastOrDefault()?.Id, partial = true })
        };

        if (partial) return PartialView("_PostList", list);

        return View("Index", new ExploreViewModel
        {
            Heading = $"\"{term}\" için sonuçlar",
            Query = term,
            Results = list,
            TrendingTags = await _posts.TrendingTagsAsync()
        });
    }
}
