using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebKoy.Web.Data;
using WebKoy.Web.Models;
using WebKoy.Web.Services;

namespace WebKoy.Web.Controllers;

public class HomeController(AppDbContext db, IOptions<PlatformOptions> options) : AppController
{
    private const int PageSize = 24;
    private readonly PlatformOptions _opt = options.Value;

    public async Task<IActionResult> Index()
    {
        var visible = db.Sites.Where(s => !s.IsBanned && s.UpdatedAt > s.CreatedAt.AddMinutes(1));
        var recent = await ToCards(visible.OrderByDescending(s => s.UpdatedAt), _opt).Take(8).ToListAsync();
        return View(new HomeViewModel
        {
            Recent = WithUrls(recent, _opt),
            SiteCount = await db.Sites.CountAsync(s => !s.IsBanned),
        });
    }

    [Route("browse")]
    public async Task<IActionResult> Browse(string sort = "updated", string? tag = null, string? q = null, int page = 1)
    {
        page = Math.Max(1, page);
        var query = db.Sites.Where(s => !s.IsBanned);

        if (!string.IsNullOrWhiteSpace(tag))
        {
            tag = tag.Trim().ToLowerInvariant();
            query = query.Where(s => s.Tags.Any(t => t.Tag.Name == tag));
        }
        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(s => s.Name.Contains(q) || (s.Title != null && s.Title.Contains(q)) || (s.Description != null && s.Description.Contains(q)));
        }

        query = sort switch
        {
            "views" => query.OrderByDescending(s => s.Views),
            "followers" => query.OrderByDescending(s => s.Followers.Count).ThenByDescending(s => s.Views),
            "likes" => query.OrderByDescending(s => s.Likes.Count).ThenByDescending(s => s.Views),
            "new" => query.OrderByDescending(s => s.CreatedAt),
            _ => query.OrderByDescending(s => s.UpdatedAt),
        };

        var total = await query.CountAsync();
        var sites = await ToCards(query, _opt).Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();
        var popularTags = await db.Tags.OrderByDescending(t => t.Sites.Count).Take(20).Select(t => t.Name).ToListAsync();

        return View(new BrowseViewModel
        {
            Sites = WithUrls(sites, _opt),
            Sort = sort,
            Tag = tag,
            Query = q,
            Page = page,
            TotalPages = (int)Math.Ceiling(total / (double)PageSize),
            PopularTags = popularTags,
        });
    }

    [Route("terms")]
    public IActionResult Terms() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
