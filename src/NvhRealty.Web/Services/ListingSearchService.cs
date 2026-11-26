using Microsoft.EntityFrameworkCore;
using NvhRealty.Web.Data;
using NvhRealty.Web.Models.Entities;
using NvhRealty.Web.Models.ViewModels;

namespace NvhRealty.Web.Services;

public class ListingSearchService
{
    private readonly AppDbContext _db;
    public ListingSearchService(AppDbContext db) => _db = db;

    public IQueryable<Listing> BuildQuery(ListingSearchQuery q)
    {
        var x = _db.Listings.AsNoTracking()
            .Include(l => l.Neighborhood).ThenInclude(n => n!.Area)
            .Include(l => l.Photos)
            .AsQueryable();

        if (q.Type is not null) x = x.Where(l => l.ListingType == q.Type);

        // Durum filtresi
        var statuses = new List<ListingStatus> { ListingStatus.Active };
        if (q.IncludePending) statuses.Add(ListingStatus.ApplicationPending);
        if (q.IncludeInContract) statuses.Add(ListingStatus.InContract);
        if (q.Sold) statuses.Add(ListingStatus.Sold);
        if (q.Rented) statuses.Add(ListingStatus.Rented);
        x = x.Where(l => statuses.Contains(l.Status));

        // Kaynak: MLS / Şirket
        if (q.Mls && !q.Company) x = x.Where(l => l.Source == ListingSource.Mls);
        else if (!q.Mls && q.Company) x = x.Where(l => l.Source == ListingSource.Company);
        if (q.Exclusive) x = x.Where(l => l.IsExclusive);
        if (q.NewListings) { var since = DateTime.UtcNow.AddDays(-14); x = x.Where(l => l.ListedOn >= since); }

        if (q.Hoods.Count > 0) x = x.Where(l => q.Hoods.Contains(l.NeighborhoodId));
        if (q.MinBeds is not null) x = x.Where(l => l.Bedrooms >= q.MinBeds);
        if (q.MaxBeds is not null) x = x.Where(l => l.Bedrooms <= q.MaxBeds);
        if (q.MinBaths is not null) x = x.Where(l => l.Bathrooms >= q.MinBaths);
        if (q.MinFamilies is not null) x = x.Where(l => l.Families >= q.MinFamilies);
        if (q.MinPrice is not null) x = x.Where(l => l.Price >= q.MinPrice);
        if (q.MaxPrice is not null) x = x.Where(l => l.Price <= q.MaxPrice);
        if (q.MinSqft is not null) x = x.Where(l => l.SquareFeet >= q.MinSqft);
        if (q.MaxMonthly is not null) x = x.Where(l => l.MonthlyExpense <= q.MaxMonthly);

        if (q.Commercial) x = x.Where(l => (int)l.PropertyType >= 10);
        if (q.PropertyTypes.Count > 0) x = x.Where(l => q.PropertyTypes.Contains(l.PropertyType));

        if (q.Doorman) x = x.Where(l => l.HasDoorman);
        if (q.Elevator) x = x.Where(l => l.HasElevator);
        if (q.Laundry) x = x.Where(l => l.HasLaundry);
        if (q.Pool) x = x.Where(l => l.HasPool);
        if (q.Outdoor) x = x.Where(l => l.HasOutdoorSpace);
        if (q.Parking) x = x.Where(l => l.HasParking);
        if (q.LaundryInUnit) x = x.Where(l => l.LaundryInUnit);
        if (q.NoFee) x = x.Where(l => l.NoFee);
        if (q.PrivateOutdoor) x = x.Where(l => l.PrivateOutdoorSpace);
        if (q.ShortTerm) x = x.Where(l => l.IsShortTerm);
        if (q.OpenHouse) { var now = DateTime.UtcNow.Date; x = x.Where(l => l.OpenHouseAt != null && l.OpenHouseAt >= now); }
        if (q.VirtualTour) x = x.Where(l => l.VirtualTourUrl != null && l.VirtualTourUrl != "");
        if (q.CentralAc) x = x.Where(l => l.CentralAirConditioning);
        if (q.Cats) x = x.Where(l => l.CatsOk);
        if (q.Dogs) x = x.Where(l => l.DogsOk);
        if (q.AgentId is not null) x = x.Where(l => l.AgentId == q.AgentId);

        return q.Sort switch
        {
            "price_asc" => x.OrderBy(l => l.Price),
            "price_desc" => x.OrderByDescending(l => l.Price),
            "beds_asc" => x.OrderBy(l => l.Bedrooms).ThenBy(l => l.Price),
            "beds_desc" => x.OrderByDescending(l => l.Bedrooms).ThenBy(l => l.Price),
            "oldest" => x.OrderBy(l => l.ListedOn),
            _ => x.OrderByDescending(l => l.ListedOn)
        };
    }

    public async Task<ListingSearchResult> SearchAsync(ListingSearchQuery q, string heading)
    {
        q.Page = Math.Max(1, q.Page);
        q.PageSize = q.View == "map" ? 200 : Math.Clamp(q.PageSize, 6, 48);
        var query = BuildQuery(q);
        var total = await query.CountAsync();
        var items = await query.Skip((q.Page - 1) * q.PageSize).Take(q.PageSize).AsSplitQuery().ToListAsync();
        var areas = await _db.Areas.AsNoTracking().Include(a => a.Neighborhoods.OrderBy(n => n.Name)).OrderBy(a => a.SortOrder).ToListAsync();
        return new ListingSearchResult { Query = q, Items = items, TotalCount = total, Areas = areas, Heading = heading };
    }
}
