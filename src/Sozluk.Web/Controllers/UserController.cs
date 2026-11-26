using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sozluk.Web.Data;
using Sozluk.Web.Models;
using Sozluk.Web.Services;

namespace Sozluk.Web.Controllers;

public class UserController(SozlukDbContext db) : Controller
{
    private const int PageSize = 10;

    /// <summary>Yazar profili: /yazar/{nick}?tab=entryler|favoriler|begenilenler</summary>
    [HttpGet("/yazar/{nick}")]
    public async Task<IActionResult> Profile(string nick, string tab = "entryler", int p = 1)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.UserName == nick);
        if (user == null) return NotFound();

        IQueryable<Entry> q = tab switch
        {
            "favoriler" => db.Favorites.Where(f => f.UserId == user.Id)
                .OrderByDescending(f => f.CreatedAt).Select(f => f.Entry!).Visible(),
            "begenilenler" => db.Entries.Visible().Where(e => e.AuthorId == user.Id)
                .OrderByDescending(e => e.UpVotes - e.DownVotes),
            _ => db.Entries.Visible().Where(e => e.AuthorId == user.Id).OrderByDescending(e => e.CreatedAt),
        };

        var total = await q.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        p = Math.Clamp(p, 1, totalPages);

        var mine = db.Entries.Where(e => e.AuthorId == user.Id && !e.IsDeleted);
        return View(new UserProfileViewModel
        {
            User = user,
            Tab = tab,
            EntryCount = await mine.CountAsync(),
            TopicCount = await db.Topics.CountAsync(t => t.CreatedById == user.Id),
            TotalUp = await mine.SumAsync(e => (int?)e.UpVotes) ?? 0,
            Entries = await q.Skip((p - 1) * PageSize).Take(PageSize).ToViews(db, User.Id()).ToListAsync(),
            Page = p,
            TotalPages = totalPages
        });
    }

    [Authorize, HttpGet("/ayarlar")]
    public async Task<IActionResult> Settings()
    {
        var user = await db.Users.FindAsync(User.Id());
        return user == null ? NotFound() : View(user);
    }

    [Authorize, HttpPost("/ayarlar")]
    public async Task<IActionResult> Settings(string? bio)
    {
        var user = await db.Users.FindAsync(User.Id());
        if (user == null) return NotFound();
        bio = bio?.Trim();
        user.Bio = string.IsNullOrEmpty(bio) ? null : bio.Length > 1000 ? bio[..1000] : bio;
        await db.SaveChangesAsync();
        TempData["Info"] = "kaydedildi";
        return Redirect("/ayarlar");
    }
}
