using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sozluk.Web.Data;
using Sozluk.Web.Models;
using Sozluk.Web.Services;

namespace Sozluk.Web.Controllers;

public class HomeController(SozlukDbContext db) : Controller
{
    /// <summary>Ana sayfa: son 24 saatin en beğenilen entry'leri, yoksa en yeniler.</summary>
    public async Task<IActionResult> Index()
    {
        var since = DateTime.UtcNow.AddDays(-1);
        var uid = User.Id();
        var entries = await db.Entries.Visible()
            .Where(e => e.CreatedAt >= since)
            .OrderByDescending(e => e.UpVotes - e.DownVotes).ThenByDescending(e => e.CreatedAt)
            .Take(20).ToViews(db, uid).ToListAsync();

        if (entries.Count == 0)
            entries = await db.Entries.Visible().OrderByDescending(e => e.CreatedAt)
                .Take(20).ToViews(db, uid).ToListAsync();

        return View(new EntryListViewModel { Heading = "bugünün en beğenilenleri", Entries = entries });
    }

    /// <summary>Dünün en beğenilen entry'leri (debe benzeri).</summary>
    [HttpGet("/dunun-en-begenilenleri")]
    public async Task<IActionResult> Best()
    {
        var today = DateTime.UtcNow.Date;
        var entries = await db.Entries.Visible()
            .Where(e => e.CreatedAt >= today.AddDays(-1) && e.CreatedAt < today)
            .OrderByDescending(e => e.UpVotes - e.DownVotes)
            .Take(50).ToViews(db, User.Id()).ToListAsync();
        return View("Index", new EntryListViewModel { Heading = "dünün en beğenilen entry'leri", Entries = entries });
    }

    [HttpGet("/rastgele")]
    public async Task<IActionResult> Random()
    {
        var count = await db.Entries.Visible().CountAsync();
        if (count == 0) return RedirectToAction(nameof(Index));
        var id = await db.Entries.Visible().OrderBy(e => e.Id)
            .Skip(System.Random.Shared.Next(count)).Select(e => e.Id).FirstAsync();
        return Redirect($"/entry/{id}");
    }

    public IActionResult Error() => View();
}
