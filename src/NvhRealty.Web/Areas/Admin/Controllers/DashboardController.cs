using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NvhRealty.Web.Data;
using NvhRealty.Web.Models.Entities;

namespace NvhRealty.Web.Areas.Admin.Controllers;

public class DashboardController : AdminControllerBase
{
    private readonly AppDbContext _db;
    public DashboardController(AppDbContext db) => _db = db;

    [HttpGet("/admin")]
    public async Task<IActionResult> Index()
    {
        ViewBag.ActiveSales = await _db.Listings.CountAsync(l => l.ListingType == ListingType.Sale && l.Status == ListingStatus.Active);
        ViewBag.ActiveRentals = await _db.Listings.CountAsync(l => l.ListingType == ListingType.Rental && l.Status == ListingStatus.Active);
        ViewBag.Agents = await _db.Agents.CountAsync(a => a.IsActive);
        ViewBag.UnreadMessages = await _db.ContactMessages.CountAsync(m => !m.IsRead);
        ViewBag.UnreadApplications = await _db.CareerApplications.CountAsync(m => !m.IsRead);
        ViewBag.PendingTestimonials = await _db.Testimonials.CountAsync(t => !t.IsApproved);
        ViewBag.Alerts = await _db.SearchAlerts.CountAsync();
        ViewBag.Users = await _db.Users.CountAsync();
        var latest = await _db.ContactMessages.AsNoTracking().OrderByDescending(m => m.CreatedAt).Take(8).ToListAsync();
        return View(latest);
    }
}
