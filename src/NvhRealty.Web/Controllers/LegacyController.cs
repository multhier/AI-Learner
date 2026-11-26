using Microsoft.AspNetCore.Mvc;

namespace NvhRealty.Web.Controllers;

/// <summary>
/// Eski ColdFusion adreslerini (index.cfm?page=...) yeni MVC rotalarına kalıcı (301) olarak yönlendirir.
/// Rota haritası REA ile orijinal sitenin bağlantı envanterinden çıkarıldı. Böylece eski linkler ve
/// arama motoru kayıtları kırılmaz.
/// </summary>
public class LegacyController : Controller
{
    [HttpGet("index.cfm")]
    public IActionResult Index(string? page, string? cms, string? sub, int? id, string? status,
        int? isMLS, int? isCompany, int? isCommercial, int? openHouseChecked, int? ShortTerm)
    {
        string target = (page ?? "").ToLowerInvariant() switch
        {
            "" => "/",
            "agents" when id is not null => $"/agents/{id}",
            "agents" => "/agents",
            "blog" => "/blog",
            "news" => "/news",
            "careers" => "/careers",
            "contact" => "/contact",
            "owners" => "/owners",
            "testimonials" => "/testimonials",
            "links" => "/page/links",
            "neighborhoods" => "/page/neighborhoods",
            "florida" => "/page/florida",
            "northport" => "/page/north-port",
            "content" when !string.IsNullOrWhiteSpace(cms) => $"/page/{Uri.EscapeDataString(cms.ToLowerInvariant())}",
            "resources" when !string.IsNullOrWhiteSpace(sub) => $"/resources/{Uri.EscapeDataString(sub.ToLowerInvariant())}",
            "account" => "/account/login",
            "properties" => PropertiesTarget(status, isMLS, isCompany, isCommercial, openHouseChecked, ShortTerm),
            _ => "/"
        };
        return RedirectPermanent(target);
    }

    private static string PropertiesTarget(string? status, int? isMls, int? isCompany, int? isCommercial, int? openHouse, int? shortTerm)
    {
        if (openHouse == 1) return "/properties/open-house";
        if (isCommercial == 1) return "/properties/commercial";
        if (shortTerm == 1) return "/properties/short-term";
        // Orijinal durum kodları: 1,11,12 = satış; 2,21 = kiralık
        var codes = (status ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var hasSale = codes.Any(c => c is "1" or "11" or "12");
        var hasRent = codes.Any(c => c is "2" or "21");
        var path = hasSale && !hasRent ? "/properties/sales" : hasRent && !hasSale ? "/properties/rentals" : "/properties";
        var qs = new List<string>();
        if (isMls == 0) qs.Add("mls=false");
        if (isCompany == 0) qs.Add("company=false");
        return qs.Count == 0 ? path : path + "?" + string.Join("&", qs);
    }
}
