using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WebKoy.Web.Data;
using WebKoy.Web.Middleware;

namespace WebKoy.Web.Controllers;

[Authorize(Roles = DbInitializer.AdminRole), Route("admin/[action]")]
public class AdminController(AppDbContext db, IMemoryCache cache) : AppController
{
    [HttpGet("/admin")]
    public async Task<IActionResult> Index(bool resolved = false)
    {
        ViewBag.Resolved = resolved;
        ViewBag.SiteCount = await db.Sites.CountAsync();
        ViewBag.BannedCount = await db.Sites.CountAsync(s => s.IsBanned);
        var reports = await db.Reports.Include(r => r.Site).Include(r => r.Reporter)
            .Where(r => r.Resolved == resolved)
            .OrderByDescending(r => r.CreatedAt).Take(200).ToListAsync();
        return View(reports);
    }

    [HttpPost]
    public async Task<IActionResult> Resolve(int id)
    {
        await db.Reports.Where(r => r.Id == id).ExecuteUpdateAsync(r => r.SetProperty(x => x.Resolved, true));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Ban(string name, string? reason)
    {
        var site = await db.Sites.FirstOrDefaultAsync(s => s.Name == name);
        if (site is null) return NotFound();
        site.IsBanned = true;
        site.BanReason = reason?.Trim();
        await db.SaveChangesAsync();
        await db.Reports.Where(r => r.SiteId == site.Id).ExecuteUpdateAsync(r => r.SetProperty(x => x.Resolved, true));
        UserSiteMiddleware.Invalidate(cache, site.Name);
        Flash($"{site.Name} askıya alındı.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Unban(string name)
    {
        var site = await db.Sites.FirstOrDefaultAsync(s => s.Name == name);
        if (site is null) return NotFound();
        site.IsBanned = false;
        site.BanReason = null;
        await db.SaveChangesAsync();
        UserSiteMiddleware.Invalidate(cache, site.Name);
        Flash($"{site.Name} yeniden etkin.");
        return RedirectToAction("Profile", "Sites", new { name });
    }
}
