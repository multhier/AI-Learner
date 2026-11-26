using System.ComponentModel.DataAnnotations;
using NvhRealty.Web.Models.Entities;

namespace NvhRealty.Web.Models.ViewModels;

public class HomeViewModel
{
    public Agent? FeaturedAgent { get; init; }
    public IReadOnlyList<Listing> LatestSales { get; init; } = Array.Empty<Listing>();
    public IReadOnlyList<Listing> LatestRentals { get; init; } = Array.Empty<Listing>();
}

public class AgentIndexViewModel
{
    public IReadOnlyList<Agent> Management { get; init; } = Array.Empty<Agent>();
    public IReadOnlyList<Agent> Agents { get; init; } = Array.Empty<Agent>();
    public IReadOnlyList<string> Languages { get; init; } = Array.Empty<string>();
    public string? Letter { get; init; }
    public string? Language { get; init; }
    public string? Name { get; init; }
}

public class AgentDetailsViewModel
{
    public Agent Agent { get; init; } = null!;
    public IReadOnlyList<Listing> Listings { get; init; } = Array.Empty<Listing>();
    public IReadOnlyList<Testimonial> Testimonials { get; init; } = Array.Empty<Testimonial>();
    public ContactFormModel Contact { get; init; } = new();
}

public class ListingDetailsViewModel
{
    public Listing Listing { get; init; } = null!;
    public IReadOnlyList<Listing> Similar { get; init; } = Array.Empty<Listing>();
    public bool IsFavorite { get; init; }
    public ContactFormModel Contact { get; init; } = new();
}

public class BlogIndexViewModel
{
    public PostCategory Category { get; init; }
    public PagedList<Post> Posts { get; init; } = new();
    /// <summary>Arşiv: Yıl → Ay → yazılar</summary>
    public IReadOnlyList<IGrouping<int, Post>> Archive { get; init; } = Array.Empty<IGrouping<int, Post>>();
}

public class ContentPageViewModel
{
    public ContentPage Page { get; init; } = null!;
}

/// <summary>Spam botlarına karşı "honeypot" alanı (orijinal sitede captcha vardı).</summary>
public abstract class BotProtectedForm
{
    [Display(AutoGenerateField = false)]
    public string? Website { get; set; }
}

public class ContactFormModel : BotProtectedForm
{
    [Required(ErrorMessage = "Ad Soyad zorunludur."), StringLength(150), Display(Name = "Ad Soyad")]
    public string Name { get; set; } = "";
    [Required(ErrorMessage = "E-posta zorunludur."), EmailAddress(ErrorMessage = "Geçerli bir e-posta girin."), Display(Name = "E-posta")]
    public string Email { get; set; } = "";
    [Phone, StringLength(40), Display(Name = "Telefon")]
    public string? Phone { get; set; }
    [Required(ErrorMessage = "Mesaj zorunludur."), StringLength(4000), Display(Name = "Mesajınız")]
    public string Message { get; set; } = "";
    public int? AgentId { get; set; }
    public int? ListingId { get; set; }
    public string? ReturnUrl { get; set; }
}

public class OwnerFormModel : BotProtectedForm
{
    [Required(ErrorMessage = "Ad Soyad zorunludur."), StringLength(150), Display(Name = "Ad Soyad")]
    public string Name { get; set; } = "";
    [Required(ErrorMessage = "E-posta zorunludur."), EmailAddress, Display(Name = "E-posta")]
    public string Email { get; set; } = "";
    [Phone, Display(Name = "Telefon")]
    public string? Phone { get; set; }
    [Required(ErrorMessage = "Mülk adresi zorunludur."), StringLength(300), Display(Name = "Mülk Adresi")]
    public string PropertyAddress { get; set; } = "";
    [Display(Name = "Satmak mı, kiralamak mı?")]
    public ListingType ListingType { get; set; } = ListingType.Sale;
    [StringLength(4000), Display(Name = "Ek Bilgi")]
    public string? Message { get; set; }
}

public class CareerFormModel : BotProtectedForm
{
    [Required(ErrorMessage = "Ad zorunludur."), Display(Name = "Ad")] public string FirstName { get; set; } = "";
    [Required(ErrorMessage = "Soyad zorunludur."), Display(Name = "Soyad")] public string LastName { get; set; } = "";
    [Required(ErrorMessage = "E-posta zorunludur."), EmailAddress, Display(Name = "E-posta")] public string Email { get; set; } = "";
    [Phone, Display(Name = "Telefon")] public string? Phone { get; set; }
    [Display(Name = "Lisans Durumu")] public LicenseStatus LicenseStatus { get; set; } = LicenseStatus.TakingCourse;
    [StringLength(4000), Display(Name = "Kendinizden bahsedin")] public string? Message { get; set; }
}

public class AlertFormModel
{
    [Required, EmailAddress, Display(Name = "E-posta")] public string Email { get; set; } = "";
    [Required] public string QueryString { get; set; } = "";
}

public class LoginViewModel
{
    [Required(ErrorMessage = "E-posta zorunludur."), EmailAddress, Display(Name = "E-posta")] public string Email { get; set; } = "";
    [Required(ErrorMessage = "Şifre zorunludur."), DataType(DataType.Password), Display(Name = "Şifre")] public string Password { get; set; } = "";
    [Display(Name = "Beni hatırla")] public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Ad zorunludur."), Display(Name = "Ad")] public string FirstName { get; set; } = "";
    [Required(ErrorMessage = "Soyad zorunludur."), Display(Name = "Soyad")] public string LastName { get; set; } = "";
    [Required(ErrorMessage = "E-posta zorunludur."), EmailAddress, Display(Name = "E-posta")] public string Email { get; set; } = "";
    [Required(ErrorMessage = "Şifre zorunludur."), StringLength(100, MinimumLength = 8, ErrorMessage = "Şifre en az 8 karakter olmalı."), DataType(DataType.Password), Display(Name = "Şifre")]
    public string Password { get; set; } = "";
    [DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "Şifreler eşleşmiyor."), Display(Name = "Şifre (tekrar)")]
    public string ConfirmPassword { get; set; } = "";
    [Display(Name = "Hangisiyle ilgileniyorsunuz?")] public InterestType InterestedIn { get; set; } = InterestType.Rentals;
}
