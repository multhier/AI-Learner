using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sozluk.Web.Data;
using Sozluk.Web.Models;
using Sozluk.Web.Services;

namespace Sozluk.Web.Controllers;

public class EntryController(SozlukDbContext db) : Controller
{
    [HttpGet("/entry/{id:int}")]
    public async Task<IActionResult> Show(int id)
    {
        var entry = await db.Entries.Visible().Where(e => e.Id == id).ToViews(db, User.Id()).FirstOrDefaultAsync();
        if (entry == null) return NotFound();
        return View(entry);
    }

    [Authorize, HttpPost("/entry/ekle")]
    public async Task<IActionResult> Create(NewEntryInput input)
    {
        var topic = await db.Topics.FindAsync(input.TopicId);
        if (topic == null) return NotFound();
        var back = $"/baslik/{topic.Slug}--{topic.Id}";

        if (!ModelState.IsValid)
        {
            TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
            return Redirect(back);
        }
        if (topic.IsLocked && !User.IsModerator())
        {
            TempData["Error"] = "başlık kilitli";
            return Redirect(back);
        }

        var uid = User.Id()!.Value;
        if (await db.Users.AnyAsync(u => u.Id == uid && u.IsBanned)) return Forbid();

        var now = DateTime.UtcNow;
        var entry = new Entry { TopicId = topic.Id, AuthorId = uid, Content = input.Content.Trim(), CreatedAt = now };
        db.Entries.Add(entry);
        topic.EntryCount++;
        topic.LastEntryAt = now;
        await db.SaveChangesAsync();

        var count = await db.Entries.Visible().CountAsync(e => e.TopicId == topic.Id);
        var lastPage = Math.Max(1, (int)Math.Ceiling(count / 10.0));
        return Redirect($"{back}?p={lastPage}#e{entry.Id}");
    }

    [Authorize, HttpGet("/entry/{id:int}/duzenle")]
    public async Task<IActionResult> Edit(int id)
    {
        var entry = await db.Entries.Include(e => e.Topic).FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);
        if (entry == null) return NotFound();
        if (entry.AuthorId != User.Id() && !User.IsModerator()) return Forbid();
        return View(entry);
    }

    [Authorize, HttpPost("/entry/{id:int}/duzenle")]
    public async Task<IActionResult> Edit(int id, string content)
    {
        var entry = await db.Entries.Include(e => e.Topic).FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);
        if (entry == null) return NotFound();
        if (entry.AuthorId != User.Id() && !User.IsModerator()) return Forbid();

        content = (content ?? "").Trim();
        if (content.Length < 2 || content.Length > 10000)
        {
            ModelState.AddModelError("content", "entry 2-10000 karakter olmalı");
            entry.Content = content;
            return View(entry);
        }
        entry.Content = content;
        entry.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Redirect($"/entry/{entry.Id}");
    }

    [Authorize, HttpPost("/entry/{id:int}/sil")]
    public async Task<IActionResult> Delete(int id)
    {
        var entry = await db.Entries.Include(e => e.Topic).FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);
        if (entry == null) return NotFound();
        if (entry.AuthorId != User.Id() && !User.IsModerator()) return Forbid();

        entry.IsDeleted = true;
        entry.Topic!.EntryCount = Math.Max(0, entry.Topic.EntryCount - 1);
        await db.SaveChangesAsync();
        return Redirect($"/baslik/{entry.Topic.Slug}--{entry.Topic.Id}");
    }

    /// <summary>AJAX oy: value = 1 (artı), -1 (eksi). Aynı oy tekrar verilirse geri alınır.</summary>
    [Authorize, HttpPost("/entry/{id:int}/oy")]
    public async Task<IActionResult> Vote(int id, short value)
    {
        if (value != 1 && value != -1) return BadRequest();
        var uid = User.Id()!.Value;
        var entry = await db.Entries.FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);
        if (entry == null) return NotFound();
        if (entry.AuthorId == uid) return BadRequest(new { error = "kendi entry'ne oy veremezsin" });

        var vote = await db.Votes.FindAsync(id, uid);
        short mine;
        if (vote == null)
        {
            db.Votes.Add(new Vote { EntryId = id, UserId = uid, Value = value });
            Apply(entry, value, +1);
            mine = value;
        }
        else if (vote.Value == value)
        {
            db.Votes.Remove(vote);
            Apply(entry, value, -1);
            mine = 0;
        }
        else
        {
            Apply(entry, vote.Value, -1);
            Apply(entry, value, +1);
            vote.Value = value;
            mine = value;
        }
        await db.SaveChangesAsync();
        return Json(new { up = entry.UpVotes, down = entry.DownVotes, mine });

        static void Apply(Entry e, short v, int delta)
        {
            if (v > 0) e.UpVotes += delta; else e.DownVotes += delta;
        }
    }

    [Authorize, HttpPost("/entry/{id:int}/favori")]
    public async Task<IActionResult> Favorite(int id)
    {
        var uid = User.Id()!.Value;
        var entry = await db.Entries.FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);
        if (entry == null) return NotFound();

        var fav = await db.Favorites.FindAsync(id, uid);
        if (fav == null)
        {
            db.Favorites.Add(new Favorite { EntryId = id, UserId = uid });
            entry.FavoriteCount++;
        }
        else
        {
            db.Favorites.Remove(fav);
            entry.FavoriteCount = Math.Max(0, entry.FavoriteCount - 1);
        }
        await db.SaveChangesAsync();
        return Json(new { count = entry.FavoriteCount, mine = fav == null });
    }

    [Authorize, HttpPost("/entry/{id:int}/sikayet")]
    public async Task<IActionResult> Report(int id, string reason)
    {
        if (!await db.Entries.AnyAsync(e => e.Id == id)) return NotFound();
        reason = (reason ?? "").Trim();
        if (reason.Length == 0) reason = "belirtilmedi";
        if (reason.Length > 500) reason = reason[..500];
        db.Reports.Add(new Report { EntryId = id, ReporterId = User.Id()!.Value, Reason = reason });
        await db.SaveChangesAsync();
        return Json(new { ok = true });
    }
}
