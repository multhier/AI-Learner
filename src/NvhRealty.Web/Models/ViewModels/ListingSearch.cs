using NvhRealty.Web.Models.Entities;

namespace NvhRealty.Web.Models.ViewModels;

/// <summary>
/// İlan arama kriterleri. Orijinal sitedeki "More" filtre panelinin birebir karşılığı
/// (index.cfm?page=properties&amp;status=...&amp;isMLS=...&amp;isCompany=...).
/// </summary>
public class ListingSearchQuery
{
    public ListingType? Type { get; set; }
    public List<int> Hoods { get; set; } = new();
    public int? MinBeds { get; set; }
    public int? MaxBeds { get; set; }
    public decimal? MinBaths { get; set; }
    public int? MinFamilies { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? MinSqft { get; set; }
    public decimal? MaxMonthly { get; set; }
    public List<PropertyType> PropertyTypes { get; set; } = new();

    public bool Commercial { get; set; }
    public bool Mls { get; set; } = true;
    public bool Company { get; set; } = true;
    public bool Exclusive { get; set; }
    public bool NewListings { get; set; }
    public bool IncludePending { get; set; } = true;
    public bool IncludeInContract { get; set; }
    public bool Sold { get; set; }
    public bool Rented { get; set; }

    public bool Doorman { get; set; }
    public bool Elevator { get; set; }
    public bool Laundry { get; set; }
    public bool Pool { get; set; }
    public bool Outdoor { get; set; }
    public bool Parking { get; set; }
    public bool LaundryInUnit { get; set; }
    public bool NoFee { get; set; }
    public bool PrivateOutdoor { get; set; }
    public bool ShortTerm { get; set; }
    public bool OpenHouse { get; set; }
    public bool VirtualTour { get; set; }
    public bool CentralAc { get; set; }
    public bool Cats { get; set; }
    public bool Dogs { get; set; }

    public int? AgentId { get; set; }
    public string Sort { get; set; } = "recent";
    public string View { get; set; } = "list";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}

public record SortOption(string Value, string Label);

public class ListingSearchResult
{
    public ListingSearchQuery Query { get; init; } = new();
    public IReadOnlyList<Listing> Items { get; init; } = Array.Empty<Listing>();
    public int TotalCount { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)Math.Max(1, Query.PageSize));
    public IReadOnlyList<Area> Areas { get; init; } = Array.Empty<Area>();
    public string Heading { get; init; } = "İlanlar";

    public static readonly SortOption[] SortOptions =
    {
        new("price_asc", "Fiyat (Düşükten Yükseğe)"),
        new("price_desc", "Fiyat (Yüksekten Düşüğe)"),
        new("beds_asc", "Yatak Odası (Azdan Çoğa)"),
        new("beds_desc", "Yatak Odası (Çoktan Aza)"),
        new("oldest", "İlan Tarihi (Eskiden Yeniye)"),
        new("recent", "İlan Tarihi (Yeniden Eskiye)")
    };
}

public class PagedList<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)Math.Max(1, PageSize));
}
