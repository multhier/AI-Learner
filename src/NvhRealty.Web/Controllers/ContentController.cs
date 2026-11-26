using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NvhRealty.Web.Data;
using NvhRealty.Web.Models.Entities;
using NvhRealty.Web.Models.ViewModels;

namespace NvhRealty.Web.Controllers;

/// <summary>Blog ("Helpful Hints"), Haberler, Müşteri Yorumları ve CMS sayfaları.</summary>
public class ContentController : Controller
{
    private readonly AppDbContext _db;
    public ContentController(AppDbContext db) => _db = db;

    [HttpGet("blog")]
    public Task<IActionResult> Blog(int page = 1) => PostList(PostCategory.Blog, page);

    [HttpGet("news")]
    public Task<IActionResult> News(int page = 1) => PostList(PostCategory.News, page);

    private async Task<IActionResult> PostList(PostCategory category, int page)
    {
        const int pageSize = 7;
        page = Math.Max(1, page);
        var q = _db.Posts.AsNoTracking().Where(p => p.IsPublished && p.Category == category);
        var total = await q.CountAsync();
        var items = await q.OrderByDescending(p => p.PublishedOn).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        var archive = (await q.OrderByDescending(p => p.PublishedOn).Select(p => new Post { Title = p.Title, Slug = p.Slug, PublishedOn = p.PublishedOn }).ToListAsync())
            .GroupBy(p => p.PublishedOn.Year).ToList();

        return View("Blog", new BlogIndexViewModel
        {
            Category = category,
            Posts = new PagedList<Post> { Items = items, Page = page, PageSize = pageSize, TotalCount = total },
            Archive = archive
        });
    }

    [HttpGet("blog/{slug}")]
    [HttpGet("news/{slug}")]
    public async Task<IActionResult> Post(string slug)
    {
        var post = await _db.Posts.AsNoTracking().FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished);
        return post is null ? NotFound() : View(post);
    }

    [HttpGet("testimonials")]
    public async Task<IActionResult> Testimonials() =>
        View(await _db.Testimonials.AsNoTracking().Include(t => t.Agent)
            .Where(t => t.IsApproved).OrderByDescending(t => t.Date).ToListAsync());

    /// <summary>CMS sayfası: /page/about, /page/disclaimer, /page/relocation ...</summary>
    [HttpGet("page/{slug}")]
    public async Task<IActionResult> Page(string slug)
    {
        var page = await _db.ContentPages.AsNoTracking().FirstOrDefaultAsync(p => p.Slug == slug);
        return page is null ? NotFound() : View("Page", new ContentPageViewModel { Page = page });
    }

    /// <summary>Resources: /resources/buyers, /resources/sellers, /resources/renters</summary>
    [HttpGet("resources/{sub}")]
    public Task<IActionResult> Resources(string sub) => Page(sub);
}
