using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NvhRealty.Web.Data;
using NvhRealty.Web.Models.Entities;
using NvhRealty.Web.Models.ViewModels;

namespace NvhRealty.Web.Controllers;

/// <summary>Danışmanlar: alfabetik gezinme, dil ve isim filtresi, profil sayfası.</summary>
[Route("agents")]
public class AgentsController : Controller
{
    private readonly AppDbContext _db;
    public AgentsController(AppDbContext db) => _db = db;

    [HttpGet("")]
    public async Task<IActionResult> Index(string? letter, string? language, string? name)
    {
        var all = await _db.Agents.AsNoTracking().Where(a => a.IsActive)
            .OrderBy(a => a.SortOrder).ThenBy(a => a.LastName).ToListAsync();

        var languages = all.SelectMany(a => (a.Languages ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(l => l).ToList();

        IEnumerable<Agent> filtered = all;
        if (!string.IsNullOrWhiteSpace(letter))
            filtered = filtered.Where(a => a.LastName.StartsWith(letter, StringComparison.OrdinalIgnoreCase) || a.FirstName.StartsWith(letter, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(language))
            filtered = filtered.Where(a => (a.Languages ?? "").Contains(language, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(name))
            filtered = filtered.Where(a => a.FullName.Contains(name, StringComparison.OrdinalIgnoreCase));

        var list = filtered.ToList();
        return View(new AgentIndexViewModel
        {
            Management = list.Where(a => a.IsManagement).ToList(),
            Agents = list.Where(a => !a.IsManagement).OrderBy(a => a.FirstName).ToList(),
            Languages = languages,
            Letter = letter,
            Language = language,
            Name = name
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var agent = await _db.Agents.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id && a.IsActive);
        if (agent is null) return NotFound();

        var listings = await _db.Listings.AsNoTracking()
            .Include(l => l.Neighborhood).Include(l => l.Photos)
            .Where(l => l.AgentId == id && l.Status != ListingStatus.Sold && l.Status != ListingStatus.Rented)
            .OrderByDescending(l => l.ListedOn).Take(12).ToListAsync();

        var testimonials = await _db.Testimonials.AsNoTracking()
            .Where(t => t.AgentId == id && t.IsApproved).OrderByDescending(t => t.Date).ToListAsync();

        return View(new AgentDetailsViewModel
        {
            Agent = agent,
            Listings = listings,
            Testimonials = testimonials,
            Contact = new ContactFormModel { AgentId = id, ReturnUrl = Url.Action(nameof(Details), new { id }) }
        });
    }

    /// <summary>"Click to Add V-Card"</summary>
    [HttpGet("{id:int}/vcard")]
    public async Task<IActionResult> VCard(int id)
    {
        var a = await _db.Agents.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.IsActive);
        if (a is null) return NotFound();
        static string Esc(string? s) => (s ?? "").Replace("\\", "\\\\").Replace(",", "\\,").Replace(";", "\\;").Replace("\n", " ");
        var vcf = $"BEGIN:VCARD\r\nVERSION:3.0\r\nN:{Esc(a.LastName)};{Esc(a.FirstName)};;;\r\nFN:{Esc(a.FullName)}\r\nTITLE:{Esc(a.Title)}\r\nTEL;TYPE=WORK,VOICE:{Esc(a.Phone)}\r\nEMAIL:{Esc(a.Email)}\r\nEND:VCARD\r\n";
        return File(System.Text.Encoding.UTF8.GetBytes(vcf), "text/vcard", $"{a.FirstName}-{a.LastName}.vcf");
    }
}
