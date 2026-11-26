using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sozluk.Web.Data;
using Sozluk.Web.Models;
using Sozluk.Web.Services;

namespace Sozluk.Web.Controllers;

[Authorize(Policy = "Moderator")]
public class ModerationController(SozlukDbContext db) : Controller
{
    [HttpGet("/mod")]
    public async Task<IActionResult> Index()
    {
        ViewBag.Reports = await db.Reports.Where(r => !r.IsResolved)
            .Include(r => r.Entry).ThenInclude(e => e!.Topic)
            .Include(r => r.Entry).ThenInclude(e => e!.Author)
            .Include(r => r.Reporter)
            .OrderBy(r => r.CreatedAt).Take(100).ToListAsync();

        ViewBag.Caylaklar = await db.Users.Where(u => u.Role == UserRole.Caylak && !u.IsBanned)
            .OrderByDescending(u => db.Entries.Count(e => e.AuthorId == u.Id && !e.IsDeleted))
            .Take(100)
            .Select(u => new CaylakItem(u.UserName, u.CreatedAt, db.Entries.Count(e => e.AuthorId == u.Id && !e.IsDeleted)))
            .ToListAsync();

        return View();
    }

    [HttpPost("/mod/sikayet/{id:int}/kapat")]
    public async Task<IActionResult> Resolve(int id, bool deleteEntry = false)
    {
        var report = await db.Reports.Include(r => r.Entry).ThenInclude(e => e!.Topic).FirstOrDefaultAsync(r => r.Id == id);
        if (report == null) return NotFound();
        report.IsResolved = true;
        if (deleteEntry && report.Entry is { IsDeleted: false } e)
        {
            e.IsDeleted = true;
            e.Topic!.EntryCount = Math.Max(0, e.Topic.EntryCount - 1);
        }
        await db.SaveChangesAsync();
        return Redirect("/mod");
    }

    [HttpPost("/mod/yazar/{nick}/rol")]
    public async Task<IActionResult> SetRole(string nick, UserRole role)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.UserName == nick);
        if (user == null) return NotFound();
        // Moderatör yalnızca çaylak/yazar atayabilir; admin her rolü atayabilir.
        if (role >= UserRole.Moderator && User.Role() != UserRole.Admin) return Forbid();
        if (user.Role >= UserRole.Moderator && User.Role() != UserRole.Admin) return Forbid();
        user.Role = role;
        await db.SaveChangesAsync();
        return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/mod");
    }

    [HttpPost("/mod/yazar/{nick}/ban")]
    public async Task<IActionResult> ToggleBan(string nick)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.UserName == nick);
        if (user == null) return NotFound();
        if (user.Role >= UserRole.Moderator && User.Role() != UserRole.Admin) return Forbid();
        user.IsBanned = !user.IsBanned;
        await db.SaveChangesAsync();
        return Redirect(Request.Headers.Referer.FirstOrDefault() ?? "/mod");
    }
}
