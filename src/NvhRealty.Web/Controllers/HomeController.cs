using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NvhRealty.Web.Data;
using NvhRealty.Web.Models;
using NvhRealty.Web.Models.Entities;
using NvhRealty.Web.Models.ViewModels;

namespace NvhRealty.Web.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;
    public HomeController(AppDbContext db) => _db = db;

    [HttpGet("/")]
    public async Task<IActionResult> Index()
    {
        var featured = await _db.Agents.AsNoTracking()
            .Where(a => a.IsActive)
            .OrderByDescending(a => a.IsFeatured).ThenBy(a => a.SortOrder)
            .FirstOrDefaultAsync();

        IQueryable<Listing> Latest(ListingType t) => _db.Listings.AsNoTracking()
            .Include(l => l.Neighborhood).Include(l => l.Photos)
            .Where(l => l.ListingType == t && l.Status == ListingStatus.Active && l.Source == ListingSource.Company)
            .OrderByDescending(l => l.ListedOn).Take(3);

        return View(new HomeViewModel
        {
            FeaturedAgent = featured,
            LatestSales = await Latest(ListingType.Sale).ToListAsync(),
            LatestRentals = await Latest(ListingType.Rental).ToListAsync()
        });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

    [HttpGet("/home/statuscode")]
    public IActionResult HttpStatus(int code)
    {
        Response.StatusCode = code;
        ViewBag.Code = code;
        return View("StatusCode");
    }
}
