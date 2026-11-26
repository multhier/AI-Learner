using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sozluk.Web.Data;
using Sozluk.Web.Models;

namespace Sozluk.Web.ViewComponents;

/// <summary>Sol çerçeve: gündem (son 24 saatte en çok entry alan başlıklar), bugün, dün.</summary>
public class GundemViewComponent(SozlukDbContext db) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var mode = HttpContext.Request.Query["sol"].ToString();
        var now = DateTime.UtcNow;
        DateTime from, to = now;
        switch (mode)
        {
            case "dun": from = now.Date.AddDays(-1); to = now.Date; break;
            case "bugun": from = now.Date; break;
            default: mode = "gundem"; from = now.AddDays(-1); break;
        }

        var items = await db.Entries
            .Where(e => !e.IsDeleted && e.CreatedAt >= from && e.CreatedAt < to)
            .GroupBy(e => new { e.TopicId, e.Topic!.Title, e.Topic.Slug })
            .Select(g => new { g.Key.TopicId, g.Key.Title, g.Key.Slug, Count = g.Count(), Last = g.Max(x => x.CreatedAt) })
            .OrderByDescending(x => mode == "gundem" ? x.Count : 0).ThenByDescending(x => x.Last)
            .Take(50).ToListAsync();

        ViewBag.Mode = mode;
        return View(items.Select(x => new GundemItem(x.TopicId, x.Title, x.Slug, x.Count)).ToList());
    }
}
