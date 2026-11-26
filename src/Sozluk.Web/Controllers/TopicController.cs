using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sozluk.Web.Data;
using Sozluk.Web.Models;
using Sozluk.Web.Services;

namespace Sozluk.Web.Controllers;

public class TopicController(SozlukDbContext db) : Controller
{
    private const int PageSize = 10;

    /// <summary>Başlık sayfası: /baslik/{slug}--{id}?p=2&amp;a=nice|bugun&amp;yazar=nick</summary>
    [HttpGet("/baslik/{slug}--{id:int}")]
    public async Task<IActionResult> Show(string slug, int id, int p = 1, string? a = null, string? yazar = null)
    {
        var topic = await db.Topics.FirstOrDefaultAsync(t => t.Id == id);
        if (topic == null) return NotFound();
        if (slug != topic.Slug)
            return RedirectPermanent($"/baslik/{topic.Slug}--{topic.Id}");

        var q = db.Entries.Visible().Where(e => e.TopicId == id);
        if (a == "bugun") q = q.Where(e => e.CreatedAt >= DateTime.UtcNow.Date);
        if (!string.IsNullOrWhiteSpace(yazar)) q = q.Where(e => e.Author!.UserName == yazar);

        var total = await q.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        p = Math.Clamp(p, 1, totalPages);

        var ordered = a == "nice"
            ? q.OrderByDescending(e => e.UpVotes - e.DownVotes).ThenBy(e => e.Id)
            : q.OrderBy(e => e.Id);

        var entries = await ordered.Skip((p - 1) * PageSize).Take(PageSize)
            .ToViews(db, User.Id()).ToListAsync();

        if (a != "nice")
            entries = entries.Select((e, i) => e with { Number = (p - 1) * PageSize + i + 1 }).ToList();

        return View(new TopicPageViewModel
        {
            Topic = topic, Entries = entries, Page = p, TotalPages = totalPages,
            Sort = a ?? "", Author = yazar, NewEntry = new NewEntryInput { TopicId = id }
        });
    }

    /// <summary>Arama / başlığa git. Tam eşleşme varsa başlığa yönlendirir.</summary>
    [HttpGet("/ara")]
    public async Task<IActionResult> Search(string? q, string? mode = null)
    {
        var term = Slug.NormalizeTitle(q ?? "");
        if (term.Length == 0) return RedirectToAction("Index", "Home");

        if (mode != "search")
        {
            var exact = await db.Topics.FirstOrDefaultAsync(t => t.Title == term);
            if (exact != null) return Redirect($"/baslik/{exact.Slug}--{exact.Id}");

            if (term.StartsWith('@'))
            {
                var nick = term[1..];
                if (await db.Users.AnyAsync(u => u.UserName == nick))
                    return Redirect($"/yazar/{Uri.EscapeDataString(nick)}");
            }
            if (term.StartsWith('#') && int.TryParse(term[1..], out var entryId))
                return Redirect($"/entry/{entryId}");
        }

        var topics = await db.Topics.Where(t => t.Title.Contains(term))
            .OrderByDescending(t => t.EntryCount).Take(50)
            .Select(t => new GundemItem(t.Id, t.Title, t.Slug, t.EntryCount)).ToListAsync();

        var canCreate = User.Identity?.IsAuthenticated == true
                        && !await db.Topics.AnyAsync(t => t.Title == term);

        return View(new SearchViewModel { Query = term, Topics = topics, CanCreate = canCreate });
    }

    [Authorize, HttpPost("/baslik/ac")]
    public async Task<IActionResult> Create(NewTopicInput input)
    {
        var title = Slug.NormalizeTitle(input.Title);
        if (!ModelState.IsValid || title.Length == 0)
        {
            TempData["Error"] = "başlık ve entry gerekli";
            return Redirect($"/ara?q={Uri.EscapeDataString(input.Title ?? "")}&mode=search");
        }

        var uid = User.Id()!.Value;
        if (await IsBannedAsync(uid)) return Forbid();

        var existing = await db.Topics.FirstOrDefaultAsync(t => t.Title == title);
        if (existing != null) return Redirect($"/baslik/{existing.Slug}--{existing.Id}");

        var now = DateTime.UtcNow;
        var topic = new Topic
        {
            Title = title, Slug = Slug.Create(title), CreatedById = uid,
            CreatedAt = now, LastEntryAt = now, EntryCount = 1
        };
        topic.Entries.Add(new Entry { AuthorId = uid, Content = input.Content.Trim(), CreatedAt = now });
        db.Topics.Add(topic);
        await db.SaveChangesAsync();
        return Redirect($"/baslik/{topic.Slug}--{topic.Id}");
    }

    [Authorize(Policy = "Moderator"), HttpPost("/baslik/{id:int}/kilit")]
    public async Task<IActionResult> ToggleLock(int id)
    {
        var topic = await db.Topics.FindAsync(id);
        if (topic == null) return NotFound();
        topic.IsLocked = !topic.IsLocked;
        await db.SaveChangesAsync();
        return Redirect($"/baslik/{topic.Slug}--{topic.Id}");
    }

    private Task<bool> IsBannedAsync(int uid) => db.Users.AnyAsync(u => u.Id == uid && u.IsBanned);
}
