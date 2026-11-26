using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NvhRealty.Web.Data;
using NvhRealty.Web.Models.Entities;
using NvhRealty.Web.Models.ViewModels;
using NvhRealty.Web.Services;

namespace NvhRealty.Web.Controllers;

/// <summary>
/// İlanlar. Menüdeki "Properties" alt başlıklarının karşılığı:
/// Sales, Rentals, Commercial, Open House, Short Term.
/// </summary>
[Route("properties")]
public class PropertiesController : Controller
{
    private readonly ListingSearchService _search;
    private readonly AppDbContext _db;

    public PropertiesController(ListingSearchService search, AppDbContext db)
    {
        _search = search;
        _db = db;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] ListingSearchQuery q) =>
        View("Index", await _search.SearchAsync(q, q.Type == ListingType.Rental ? "Kiralık İlanlar" : q.Type == ListingType.Sale ? "Satılık İlanlar" : "Tüm İlanlar"));

    [HttpGet("sales")]
    public Task<IActionResult> Sales([FromQuery] ListingSearchQuery q) { q.Type = ListingType.Sale; return Run(q, "Satılık İlanlar"); }

    [HttpGet("rentals")]
    public Task<IActionResult> Rentals([FromQuery] ListingSearchQuery q) { q.Type = ListingType.Rental; return Run(q, "Kiralık İlanlar"); }

    [HttpGet("commercial")]
    public Task<IActionResult> Commercial([FromQuery] ListingSearchQuery q) { q.Commercial = true; return Run(q, "Ticari Gayrimenkul"); }

    [HttpGet("open-house")]
    public Task<IActionResult> OpenHouse([FromQuery] ListingSearchQuery q) { q.OpenHouse = true; q.Mls = false; return Run(q, "Açık Ev Günleri"); }

    [HttpGet("short-term")]
    public Task<IActionResult> ShortTerm([FromQuery] ListingSearchQuery q) { q.Type = ListingType.Rental; q.ShortTerm = true; return Run(q, "Kısa Dönem Kiralık"); }

    private async Task<IActionResult> Run(ListingSearchQuery q, string heading) =>
        View("Index", await _search.SearchAsync(q, heading));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var listing = await _db.Listings.AsNoTracking()
            .Include(l => l.Neighborhood).ThenInclude(n => n!.Area)
            .Include(l => l.Agent)
            .Include(l => l.Photos.OrderBy(p => p.SortOrder))
            .FirstOrDefaultAsync(l => l.Id == id);
        if (listing is null) return NotFound();

        var similar = await _db.Listings.AsNoTracking()
            .Include(l => l.Neighborhood).Include(l => l.Photos)
            .Where(l => l.Id != id && l.ListingType == listing.ListingType && l.Status == ListingStatus.Active
                        && l.Neighborhood!.AreaId == listing.Neighborhood!.AreaId)
            .OrderBy(l => Math.Abs(l.Bedrooms - listing.Bedrooms)).ThenByDescending(l => l.ListedOn)
            .Take(3).ToListAsync();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var fav = userId != null && await _db.FavoriteListings.AnyAsync(f => f.UserId == userId && f.ListingId == id);

        return View(new ListingDetailsViewModel
        {
            Listing = listing,
            Similar = similar,
            IsFavorite = fav,
            Contact = new ContactFormModel
            {
                ListingId = id,
                AgentId = listing.AgentId,
                Message = $"{listing.DisplayTitle} ilanı hakkında bilgi almak istiyorum.",
                ReturnUrl = Url.Action(nameof(Details), new { id })
            }
        });
    }

    [Authorize, HttpPost("{id:int}/favorite"), ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleFavorite(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await _db.Listings.AnyAsync(l => l.Id == id)) return NotFound();
        var fav = await _db.FavoriteListings.FindAsync(userId, id);
        if (fav is null) _db.FavoriteListings.Add(new FavoriteListing { UserId = userId, ListingId = id });
        else _db.FavoriteListings.Remove(fav);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>"Create Alert" — mevcut aramayı e-posta uyarısı olarak kaydeder.</summary>
    [HttpPost("alert"), ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAlert(AlertFormModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Uyarı oluşturmak için geçerli bir e-posta girin.";
        }
        else
        {
            _db.SearchAlerts.Add(new SearchAlert
            {
                Email = model.Email,
                QueryString = model.QueryString.Length > 2000 ? model.QueryString[..2000] : model.QueryString,
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Teşekkürler! Arama uyarınız oluşturuldu.";
        }
        var back = model.QueryString.StartsWith('/') && !model.QueryString.StartsWith("//") ? model.QueryString : "/properties";
        return LocalRedirect(back);
    }
}
