using System.Diagnostics;
using Kivilcim.Bll.Abstractions;
using Kivilcim.Bll.Dtos;
using Kivilcim.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kivilcim.Web.Controllers;

public class HomeController : KivilcimController
{
    private readonly IPostQueryService _posts;
    private readonly IBlogService _blogs;

    public HomeController(IPostQueryService posts, IBlogService blogs)
    {
        _posts = posts;
        _blogs = blogs;
    }

    /// <summary>Girişliyse takip edilenlerin akışı, değilse popüler gönderiler.</summary>
    public async Task<IActionResult> Index(int? before, bool partial = false)
    {
        var me = CurrentUserId;
        if (me == null)
        {
            var popular = await _posts.PopularAsync(null);
            return View(new HomeViewModel
            {
                IsDashboard = false,
                Feed = new PostListViewModel { Posts = popular, EmptyMessage = "Henüz paylaşılan bir şey yok." },
                TrendingTags = await _posts.TrendingTagsAsync()
            });
        }

        var cards = await _posts.FeedPageAsync(me, before);
        var feed = new PostListViewModel
        {
            Posts = cards,
            EmptyMessage = "Akışın boş. Keşfet'ten birkaç blog takip etmeye ne dersin?",
            NextPageUrl = NextPageUrl(cards, _posts.PageSize, nameof(Index), null, new { before = cards.LastOrDefault()?.Id, partial = true })
        };

        if (partial)
        {
            return PartialView("_PostList", feed);
        }

        return View(new HomeViewModel
        {
            IsDashboard = true,
            Feed = feed,
            TrendingTags = await _posts.TrendingTagsAsync(),
            Suggestions = await _blogs.SuggestionsAsync(me)
        });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
