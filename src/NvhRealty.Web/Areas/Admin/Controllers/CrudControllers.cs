using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NvhRealty.Web.Data;
using NvhRealty.Web.Models.Entities;
using NvhRealty.Web.Services;

namespace NvhRealty.Web.Areas.Admin.Controllers;

public class AgentsController : AdminControllerBase
{
    private readonly AppDbContext _db;
    private readonly FileStorageService _files;
    public AgentsController(AppDbContext db, FileStorageService files) { _db = db; _files = files; }

    public async Task<IActionResult> Index() =>
        View(await _db.Agents.AsNoTracking().OrderBy(a => a.SortOrder).ThenBy(a => a.LastName).ToListAsync());

    public IActionResult Create() => View("Edit", new Agent());

    public async Task<IActionResult> Edit(int id) =>
        await _db.Agents.FindAsync(id) is { } a ? View(a) : NotFound();

    [HttpPost]
    public async Task<IActionResult> Save(Agent model, IFormFile? photo)
    {
        ModelState.Remove(nameof(Agent.Listings));
        ModelState.Remove(nameof(Agent.Testimonials));
        try
        {
            if (await _files.SaveImageAsync(photo, "agents") is string url) model.PhotoUrl = url;
        }
        catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); }
        if (!ModelState.IsValid) return View("Edit", model);

        if (model.Id == 0) _db.Agents.Add(model);
        else
        {
            var entity = await _db.Agents.FindAsync(model.Id);
            if (entity is null) return NotFound();
            _db.Entry(entity).CurrentValues.SetValues(model);
        }
        await _db.SaveChangesAsync();
        Flash("Danışman kaydedildi.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (await _db.Agents.FindAsync(id) is { } a)
        {
            _db.Agents.Remove(a);
            await _db.SaveChangesAsync();
            Flash("Danışman silindi. İlanları danışmansız olarak kaldı.");
        }
        return RedirectToAction(nameof(Index));
    }
}

public class PostsController : AdminControllerBase
{
    private readonly AppDbContext _db;
    public PostsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index() =>
        View(await _db.Posts.AsNoTracking().OrderByDescending(p => p.PublishedOn).ToListAsync());

    public IActionResult Create() => View("Edit", new Post());

    public async Task<IActionResult> Edit(int id) =>
        await _db.Posts.FindAsync(id) is { } p ? View(p) : NotFound();

    [HttpPost]
    public async Task<IActionResult> Save(Post model)
    {
        model.Slug = Slugify(string.IsNullOrWhiteSpace(model.Slug) ? model.Title : model.Slug);
        ModelState.Remove(nameof(Post.Slug));
        if (await _db.Posts.AnyAsync(p => p.Slug == model.Slug && p.Id != model.Id))
            ModelState.AddModelError(nameof(Post.Slug), "Bu kısa ad başka bir yazıda kullanılıyor.");
        if (!ModelState.IsValid) return View("Edit", model);

        if (model.Id == 0) _db.Posts.Add(model);
        else
        {
            var entity = await _db.Posts.FindAsync(model.Id);
            if (entity is null) return NotFound();
            _db.Entry(entity).CurrentValues.SetValues(model);
        }
        await _db.SaveChangesAsync();
        Flash("Yazı kaydedildi.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (await _db.Posts.FindAsync(id) is { } p) { _db.Posts.Remove(p); await _db.SaveChangesAsync(); Flash("Yazı silindi."); }
        return RedirectToAction(nameof(Index));
    }

    public static string Slugify(string text)
    {
        var map = new Dictionary<char, string> { ['ç'] = "c", ['ğ'] = "g", ['ı'] = "i", ['ö'] = "o", ['ş'] = "s", ['ü'] = "u", ['Ç'] = "c", ['Ğ'] = "g", ['İ'] = "i", ['Ö'] = "o", ['Ş'] = "s", ['Ü'] = "u" };
        var s = string.Concat(text.Select(c => map.TryGetValue(c, out var r) ? r : c.ToString())).ToLowerInvariant();
        s = Regex.Replace(s, @"[^a-z0-9]+", "-").Trim('-');
        return s.Length > 200 ? s[..200].Trim('-') : (s.Length == 0 ? Guid.NewGuid().ToString("N")[..8] : s);
    }
}

public class TestimonialsController : AdminControllerBase
{
    private readonly AppDbContext _db;
    public TestimonialsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index() =>
        View(await _db.Testimonials.AsNoTracking().Include(t => t.Agent).OrderBy(t => t.IsApproved).ThenByDescending(t => t.Date).ToListAsync());

    public async Task<IActionResult> Create() { await Lookups(); return View("Edit", new Testimonial { IsApproved = true }); }

    public async Task<IActionResult> Edit(int id)
    {
        var t = await _db.Testimonials.FindAsync(id);
        if (t is null) return NotFound();
        await Lookups();
        return View(t);
    }

    [HttpPost]
    public async Task<IActionResult> Save(Testimonial model)
    {
        ModelState.Remove(nameof(Testimonial.Agent));
        if (!ModelState.IsValid) { await Lookups(); return View("Edit", model); }
        if (model.Id == 0) _db.Testimonials.Add(model);
        else
        {
            var entity = await _db.Testimonials.FindAsync(model.Id);
            if (entity is null) return NotFound();
            _db.Entry(entity).CurrentValues.SetValues(model);
        }
        await _db.SaveChangesAsync();
        Flash("Yorum kaydedildi.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (await _db.Testimonials.FindAsync(id) is { } t) { _db.Testimonials.Remove(t); await _db.SaveChangesAsync(); Flash("Yorum silindi."); }
        return RedirectToAction(nameof(Index));
    }

    private async Task Lookups() =>
        ViewBag.Agents = new SelectList(await _db.Agents.AsNoTracking().OrderBy(a => a.FirstName).Select(a => new { a.Id, Name = a.FirstName + " " + a.LastName }).ToListAsync(), "Id", "Name");
}

public class PagesController : AdminControllerBase
{
    private readonly AppDbContext _db;
    public PagesController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index() => View(await _db.ContentPages.AsNoTracking().OrderBy(p => p.Slug).ToListAsync());

    public IActionResult Create() => View("Edit", new ContentPage());

    public async Task<IActionResult> Edit(int id) =>
        await _db.ContentPages.FindAsync(id) is { } p ? View(p) : NotFound();

    [HttpPost]
    public async Task<IActionResult> Save(ContentPage model)
    {
        model.Slug = PostsController.Slugify(model.Slug ?? "");
        if (await _db.ContentPages.AnyAsync(p => p.Slug == model.Slug && p.Id != model.Id))
            ModelState.AddModelError(nameof(ContentPage.Slug), "Bu kısa ad kullanılıyor.");
        if (!ModelState.IsValid) return View("Edit", model);
        model.UpdatedAt = DateTime.UtcNow;
        if (model.Id == 0) _db.ContentPages.Add(model);
        else
        {
            var entity = await _db.ContentPages.FindAsync(model.Id);
            if (entity is null) return NotFound();
            _db.Entry(entity).CurrentValues.SetValues(model);
        }
        await _db.SaveChangesAsync();
        Flash("Sayfa kaydedildi.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (await _db.ContentPages.FindAsync(id) is { } p) { _db.ContentPages.Remove(p); await _db.SaveChangesAsync(); Flash("Sayfa silindi."); }
        return RedirectToAction(nameof(Index));
    }
}

/// <summary>Gelen kutusu: iletişim mesajları, mülk listeleme talepleri, kariyer başvuruları, arama uyarıları.</summary>
public class MessagesController : AdminControllerBase
{
    private readonly AppDbContext _db;
    public MessagesController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string tab = "messages")
    {
        ViewBag.Tab = tab;
        ViewBag.Messages = await _db.ContactMessages.AsNoTracking().Include(m => m.Agent).Include(m => m.Listing).OrderByDescending(m => m.CreatedAt).Take(200).ToListAsync();
        ViewBag.Applications = await _db.CareerApplications.AsNoTracking().OrderByDescending(m => m.CreatedAt).Take(200).ToListAsync();
        ViewBag.Alerts = await _db.SearchAlerts.AsNoTracking().OrderByDescending(m => m.CreatedAt).Take(200).ToListAsync();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> MarkRead(int id, string kind)
    {
        if (kind == "app") { if (await _db.CareerApplications.FindAsync(id) is { } a) a.IsRead = !a.IsRead; }
        else if (await _db.ContactMessages.FindAsync(id) is { } m) m.IsRead = !m.IsRead;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index), new { tab = kind == "app" ? "applications" : "messages" });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, string kind)
    {
        switch (kind)
        {
            case "app": if (await _db.CareerApplications.FindAsync(id) is { } a) _db.CareerApplications.Remove(a); break;
            case "alert": if (await _db.SearchAlerts.FindAsync(id) is { } s) _db.SearchAlerts.Remove(s); break;
            default: if (await _db.ContactMessages.FindAsync(id) is { } m) _db.ContactMessages.Remove(m); break;
        }
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index), new { tab = kind switch { "app" => "applications", "alert" => "alerts", _ => "messages" } });
    }
}

public class SettingsController : AdminControllerBase
{
    private readonly AppDbContext _db;
    private readonly SiteSettingsService _settings;
    public SettingsController(AppDbContext db, SiteSettingsService settings) { _db = db; _settings = settings; }

    public async Task<IActionResult> Index() => View(await _settings.GetAllAsync());

    [HttpPost]
    public async Task<IActionResult> Index(Dictionary<string, string?> values)
    {
        foreach (var key in SiteSettingsService.Defaults.Keys)
        {
            if (!values.TryGetValue(key, out var v)) continue;
            var entity = await _db.SiteSettings.FindAsync(key);
            if (entity is null) _db.SiteSettings.Add(new SiteSetting { Key = key, Value = v });
            else entity.Value = v;
        }
        await _db.SaveChangesAsync();
        _settings.Invalidate();
        Flash("Ayarlar kaydedildi.");
        return RedirectToAction(nameof(Index));
    }
}
