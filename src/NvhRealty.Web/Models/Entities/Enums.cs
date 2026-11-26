using System.ComponentModel.DataAnnotations;

namespace NvhRealty.Web.Models.Entities;

/// <summary>Satılık mı kiralık mı? (Orijinal sitede status=1,11,12 = satış, 2,21 = kiralık)</summary>
public enum ListingType
{
    [Display(Name = "Satılık")] Sale = 1,
    [Display(Name = "Kiralık")] Rental = 2
}

public enum ListingStatus
{
    [Display(Name = "Aktif")] Active = 1,
    [Display(Name = "Onay Bekliyor")] ApplicationPending = 2,
    [Display(Name = "Sözleşmede")] InContract = 3,
    [Display(Name = "Satıldı")] Sold = 4,
    [Display(Name = "Kiralandı")] Rented = 5
}

public enum PropertyType
{
    [Display(Name = "Condo")] Condo = 1,
    [Display(Name = "Co-op")] Coop = 2,
    [Display(Name = "Townhouse")] Townhouse = 3,
    [Display(Name = "Müstakil Ev")] House = 4,
    [Display(Name = "Ticari")] Commercial = 10,
    [Display(Name = "Mağaza")] Retail = 11,
    [Display(Name = "Ofis")] Office = 12,
    [Display(Name = "Bina")] Building = 13,
    [Display(Name = "Arsa")] Land = 14,
    [Display(Name = "Yatırım")] Investment = 15
}

public enum ListingSource
{
    [Display(Name = "Şirket İlanı")] Company = 1,
    [Display(Name = "MLS İlanı")] Mls = 2
}

public enum PostCategory
{
    [Display(Name = "Faydalı İpuçları (Blog)")] Blog = 1,
    [Display(Name = "Son Haberler")] News = 2
}

public enum MessageType
{
    [Display(Name = "İletişim Formu")] Contact = 1,
    [Display(Name = "Danışmana Mesaj")] AgentContact = 2,
    [Display(Name = "İlan Hakkında Bilgi")] ListingInquiry = 3,
    [Display(Name = "Mülkünü Listele (Owners)")] ListWithUs = 4
}

public enum LicenseStatus
{
    [Display(Name = "Kursa Devam Ediyor")] TakingCourse = 1,
    [Display(Name = "Lisanslı")] Licensed = 2,
    [Display(Name = "Süresi Dolmuş")] Expired = 3
}

public enum InterestType
{
    [Display(Name = "Kiralık")] Rentals = 1,
    [Display(Name = "Satılık")] Sales = 2
}
