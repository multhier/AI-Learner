using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NvhRealty.Web.Data;
using NvhRealty.Web.Models.Entities;
using NvhRealty.Web.Services;

namespace NvhRealty.Web.Areas.Admin.Controllers;

public class ListingsController : AdminControllerBase
{
    private readonly AppDbContext _db;
    private readonly FileStorageService _files;

    public ListingsController(AppDbContext db, FileStorageService files)
    {
        _db = db;
        _files = files;
    }

    public async Task<IActionResult> Index(string? q, ListingType? type, ListingStatus? status, int page = 1)
    {
        const int pageSize = 25;
        var x = _db.Listings.AsNoTracking().Include(l => l.Neighborhood).Include(l => l.Agent).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) x = x.Where(l => l.Address.Contains(q) || (l.Unit != null && l.Unit.Contains(q)));
        if (type is not null) x = x.Where(l => l.ListingType == type);
        if (status is not null) x = x.Where(l => l.Status == status);
        ViewBag.Total = await x.CountAsync();
        ViewBag.Page = Math.Max(1, page);
        ViewBag.PageSize = pageSize;
        ViewBag.Q = q; ViewBag.Type = type; ViewBag.Status = status;
        return View(await x.OrderByDescending(l => l.ListedOn).Skip((Math.Max(1, page) - 1) * pageSize).Take(pageSize).ToListAsync());
    }

    public async Task<IActionResult> Create()
    {
        await LoadLookupsAsync();
        return View("Edit", new Listing { ListedOn = DateTime.UtcNow });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var listing = await _db.Listings.Include(l => l.Photos.OrderBy(p => p.SortOrder)).FirstOrDefaultAsync(l => l.Id == id);
        if (listing is null) return NotFound();
        await LoadLookupsAsync();
        return View(listing);
    }

    [HttpPost]
    public async Task<IActionResult> Save(Listing model, string? photoUrls, List<IFormFile>? uploads)
    {
        ModelState.Remove(nameof(Listing.Neighborhood));
        ModelState.Remove(nameof(Listing.Agent));
        if (!await _db.Neighborhoods.AnyAsync(n => n.Id == model.NeighborhoodId))
            ModelState.AddModelError(nameof(Listing.NeighborhoodId), "Semt seçin.");
        if (model.AgentId is int aid && !await _db.Agents.AnyAsync(a => a.Id == aid))
            ModelState.AddModelError(nameof(Listing.AgentId), "Geçersiz danışman.");

        var urls = (photoUrls ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        try
        {
            foreach (var f in uploads ?? new())
                if (await _files.SaveImageAsync(f, "listings") is string u) urls.Add(u);
        }
        catch (InvalidOperationException ex) { ModelState.AddModelError("", ex.Message); }

        if (!ModelState.IsValid)
        {
            model.Photos = urls.Select((u, i) => new ListingPhoto { Url = u, SortOrder = i }).ToList();
            await LoadLookupsAsync();
            return View("Edit", model);
        }

        Listing entity;
        if (model.Id == 0)
        {
            entity = model;
            _db.Listings.Add(entity);
        }
        else
        {
            entity = await _db.Listings.Include(l => l.Photos).FirstOrDefaultAsync(l => l.Id == model.Id) ?? throw new KeyNotFoundException();
            _db.Entry(entity).CurrentValues.SetValues(model);
            _db.ListingPhotos.RemoveRange(entity.Photos);
        }
        entity.Photos = urls.Select((u, i) => new ListingPhoto { Url = u, SortOrder = i }).ToList();
        await _db.SaveChangesAsync();
        Flash("İlan kaydedildi.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var listing = await _db.Listings.FindAsync(id);
        if (listing != null)
        {
            _db.FavoriteListings.RemoveRange(_db.FavoriteListings.Where(f => f.ListingId == id));
            _db.Listings.Remove(listing);
            await _db.SaveChangesAsync();
            Flash("İlan silindi.");
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadLookupsAsync()
    {
        var hoods = await _db.Neighborhoods.AsNoTracking().Include(n => n.Area).OrderBy(n => n.Area!.SortOrder).ThenBy(n => n.Name).ToListAsync();
        // Aynı bölgedeki semtler aynı SelectListGroup nesnesini paylaşmalı (optgroup)
        var groups = hoods.Select(h => h.Area!.Name).Distinct().ToDictionary(n => n, n => new SelectListGroup { Name = n });
        ViewBag.Neighborhoods = hoods.Select(n => new SelectListItem(n.Name, n.Id.ToString()) { Group = groups[n.Area!.Name] }).ToList();
        ViewBag.Agents = new SelectList(await _db.Agents.AsNoTracking().OrderBy(a => a.FirstName).Select(a => new { a.Id, Name = a.FirstName + " " + a.LastName }).ToListAsync(), "Id", "Name");
    }
}
