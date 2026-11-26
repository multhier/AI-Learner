using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace NvhRealty.Web.Models.Entities;

/// <summary>Bölge (Manhattan, Brooklyn, Queens ...)</summary>
public class Area
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string Name { get; set; } = "";
    public int SortOrder { get; set; }
    public List<Neighborhood> Neighborhoods { get; set; } = new();
}

/// <summary>Semt (Upper East Side, Astoria ...)</summary>
public class Neighborhood
{
    public int Id { get; set; }
    public int AreaId { get; set; }
    public Area? Area { get; set; }
    [Required, StringLength(100)] public string Name { get; set; } = "";
}

public class Agent
{
    public int Id { get; set; }
    [Required, StringLength(80), Display(Name = "Ad")] public string FirstName { get; set; } = "";
    [Required, StringLength(80), Display(Name = "Soyad")] public string LastName { get; set; } = "";
    [StringLength(120), Display(Name = "Ünvan")] public string? Title { get; set; }
    [EmailAddress, StringLength(200), Display(Name = "E-posta")] public string? Email { get; set; }
    [StringLength(40), Display(Name = "Telefon")] public string? Phone { get; set; }
    [Display(Name = "Biyografi")] public string? Bio { get; set; }
    [StringLength(400), Display(Name = "Fotoğraf URL")] public string? PhotoUrl { get; set; }
    /// <summary>Virgülle ayrılmış diller: "English, Russian"</summary>
    [StringLength(300), Display(Name = "Konuşulan Diller")] public string? Languages { get; set; }
    [Display(Name = "Yönetim Kadrosu")] public bool IsManagement { get; set; }
    [Display(Name = "Anasayfada Göster")] public bool IsFeatured { get; set; }
    [Display(Name = "Aktif")] public bool IsActive { get; set; } = true;
    [Display(Name = "Sıra")] public int SortOrder { get; set; }

    [NotMapped] public string FullName => $"{FirstName} {LastName}";
    public List<Listing> Listings { get; set; } = new();
    public List<Testimonial> Testimonials { get; set; } = new();
}

public class Listing
{
    public int Id { get; set; }
    [Required, StringLength(200), Display(Name = "Adres")] public string Address { get; set; } = "";
    [StringLength(30), Display(Name = "Daire No")] public string? Unit { get; set; }
    [Display(Name = "Semt")] public int NeighborhoodId { get; set; }
    public Neighborhood? Neighborhood { get; set; }
    [Display(Name = "Danışman")] public int? AgentId { get; set; }
    public Agent? Agent { get; set; }

    [Display(Name = "İlan Tipi")] public ListingType ListingType { get; set; } = ListingType.Sale;
    [Display(Name = "Durum")] public ListingStatus Status { get; set; } = ListingStatus.Active;
    [Display(Name = "Mülk Tipi")] public PropertyType PropertyType { get; set; } = PropertyType.Condo;
    [Display(Name = "Kaynak")] public ListingSource Source { get; set; } = ListingSource.Company;
    [Display(Name = "Münhasır (Exclusive)")] public bool IsExclusive { get; set; }

    [Column(TypeName = "decimal(18,2)"), Display(Name = "Fiyat ($)")] public decimal Price { get; set; }
    [Display(Name = "Yatak Odası (0 = Stüdyo)")] public int Bedrooms { get; set; }
    [Column(TypeName = "decimal(4,1)"), Display(Name = "Banyo")] public decimal Bathrooms { get; set; }
    [Display(Name = "Aile Sayısı (Families)")] public int? Families { get; set; }
    [Display(Name = "Metrekare (sq ft)")] public int? SquareFeet { get; set; }
    [Column(TypeName = "decimal(18,2)"), Display(Name = "Aylık Gider ($)")] public decimal? MonthlyExpense { get; set; }
    [Display(Name = "Açıklama")] public string? Description { get; set; }
    [StringLength(200), Display(Name = "İlan Sahibi (Courtesy Of)")] public string? CourtesyOf { get; set; }

    // Bina özellikleri
    [Display(Name = "Kapıcı")] public bool HasDoorman { get; set; }
    [Display(Name = "Asansör")] public bool HasElevator { get; set; }
    [Display(Name = "Çamaşırhane")] public bool HasLaundry { get; set; }
    [Display(Name = "Havuz")] public bool HasPool { get; set; }
    [Display(Name = "Açık Alan")] public bool HasOutdoorSpace { get; set; }
    [Display(Name = "Otopark")] public bool HasParking { get; set; }
    // Daire özellikleri
    [Display(Name = "Dairede Çamaşır Makinesi")] public bool LaundryInUnit { get; set; }
    [Display(Name = "Komisyonsuz (No Fee)")] public bool NoFee { get; set; }
    [Display(Name = "Özel Açık Alan")] public bool PrivateOutdoorSpace { get; set; }
    [Display(Name = "Kısa Dönem")] public bool IsShortTerm { get; set; }
    [Display(Name = "Merkezi Klima")] public bool CentralAirConditioning { get; set; }
    [Display(Name = "Kedi Olur")] public bool CatsOk { get; set; }
    [Display(Name = "Köpek Olur")] public bool DogsOk { get; set; }

    [StringLength(400), Display(Name = "Sanal Tur URL")] public string? VirtualTourUrl { get; set; }
    [Display(Name = "Açık Ev Tarihi")] public DateTime? OpenHouseAt { get; set; }
    [Display(Name = "Müsaitlik Tarihi")] public DateTime? AvailableOn { get; set; }
    [Display(Name = "İlan Tarihi")] public DateTime ListedOn { get; set; } = DateTime.UtcNow;
    [Column(TypeName = "decimal(9,6)")] public decimal? Latitude { get; set; }
    [Column(TypeName = "decimal(9,6)")] public decimal? Longitude { get; set; }

    public List<ListingPhoto> Photos { get; set; } = new();

    [NotMapped] public bool IsCommercial => (int)PropertyType >= 10;
    [NotMapped] public string DisplayTitle =>
        (IsCommercial ? PropertyType.ToString() : Bedrooms == 0 ? "Stüdyo" : $"{Bedrooms} BR") + $" at {Address}" + (string.IsNullOrEmpty(Unit) ? "" : $", #{Unit}");
}

public class ListingPhoto
{
    public int Id { get; set; }
    public int ListingId { get; set; }
    public Listing? Listing { get; set; }
    [Required, StringLength(400)] public string Url { get; set; } = "";
    public int SortOrder { get; set; }
}

public class Post
{
    public int Id { get; set; }
    [Display(Name = "Kategori")] public PostCategory Category { get; set; } = PostCategory.Blog;
    [Required, StringLength(250), Display(Name = "Başlık")] public string Title { get; set; } = "";
    [Required, StringLength(250)] public string Slug { get; set; } = "";
    [StringLength(150), Display(Name = "Yazar")] public string? Author { get; set; }
    [Display(Name = "Yayın Tarihi")] public DateTime PublishedOn { get; set; } = DateTime.UtcNow;
    [StringLength(600), Display(Name = "Özet")] public string? Summary { get; set; }
    [Display(Name = "İçerik (HTML)")] public string Body { get; set; } = "";
    [StringLength(400), Display(Name = "Kapak Görseli URL")] public string? ImageUrl { get; set; }
    [Display(Name = "Yayında")] public bool IsPublished { get; set; } = true;
}

public class Testimonial
{
    public int Id { get; set; }
    [Display(Name = "Danışman")] public int? AgentId { get; set; }
    public Agent? Agent { get; set; }
    [Required, StringLength(150), Display(Name = "Müşteri Adı")] public string ClientName { get; set; } = "";
    [StringLength(200), Display(Name = "Başlık")] public string? Headline { get; set; }
    [StringLength(120), Display(Name = "Semt / Konum")] public string? Location { get; set; }
    [Required, Display(Name = "Yorum")] public string Body { get; set; } = "";
    [Display(Name = "Tarih")] public DateTime Date { get; set; } = DateTime.UtcNow;
    [Display(Name = "Onaylı")] public bool IsApproved { get; set; }
}

/// <summary>CMS sayfası. Orijinal: index.cfm?page=content&amp;cms=about</summary>
public class ContentPage
{
    public int Id { get; set; }
    [Required, StringLength(100), Display(Name = "Kısa Ad (slug)")] public string Slug { get; set; } = "";
    [Required, StringLength(200), Display(Name = "Başlık")] public string Title { get; set; } = "";
    [Display(Name = "İçerik (HTML)")] public string Body { get; set; } = "";
    [StringLength(400), Display(Name = "Video URL")] public string? VideoUrl { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class ContactMessage
{
    public int Id { get; set; }
    public MessageType Type { get; set; } = MessageType.Contact;
    [Required, StringLength(150)] public string Name { get; set; } = "";
    [Required, EmailAddress, StringLength(200)] public string Email { get; set; } = "";
    [StringLength(40)] public string? Phone { get; set; }
    [StringLength(4000)] public string? Message { get; set; }
    public int? AgentId { get; set; }
    public Agent? Agent { get; set; }
    public int? ListingId { get; set; }
    public Listing? Listing { get; set; }
    [StringLength(300)] public string? PropertyAddress { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class CareerApplication
{
    public int Id { get; set; }
    [Required, StringLength(80)] public string FirstName { get; set; } = "";
    [Required, StringLength(80)] public string LastName { get; set; } = "";
    [Required, EmailAddress, StringLength(200)] public string Email { get; set; } = "";
    [StringLength(40)] public string? Phone { get; set; }
    public LicenseStatus LicenseStatus { get; set; } = LicenseStatus.TakingCourse;
    [StringLength(4000)] public string? Message { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>"Create Alert" — kayıtlı arama</summary>
public class SearchAlert
{
    public int Id { get; set; }
    [Required, EmailAddress, StringLength(200)] public string Email { get; set; } = "";
    [Required, StringLength(2000)] public string QueryString { get; set; } = "";
    public string? UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class FavoriteListing
{
    public string UserId { get; set; } = "";
    public ApplicationUser? User { get; set; }
    public int ListingId { get; set; }
    public Listing? Listing { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Site geneli ayarlar (şirket adı, adres, telefon, sosyal medya)</summary>
public class SiteSetting
{
    [Key, StringLength(100)] public string Key { get; set; } = "";
    [StringLength(2000)] public string? Value { get; set; }
}

public class ApplicationUser : IdentityUser
{
    [StringLength(80)] public string? FirstName { get; set; }
    [StringLength(80)] public string? LastName { get; set; }
    public InterestType? InterestedIn { get; set; }
    public List<FavoriteListing> Favorites { get; set; } = new();
}
