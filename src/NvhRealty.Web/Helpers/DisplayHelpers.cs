using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using NvhRealty.Web.Models.Entities;

namespace NvhRealty.Web.Helpers;

public static class DisplayHelpers
{
    private static readonly CultureInfo Us = CultureInfo.GetCultureInfo("en-US");

    public static string DisplayName(this Enum value) =>
        value.GetType().GetMember(value.ToString()).FirstOrDefault()?
            .GetCustomAttribute<DisplayAttribute>()?.GetName() ?? value.ToString();

    public static string Money(this decimal value) => value.ToString("C0", Us);

    public static string PriceLabel(this Listing l) =>
        l.ListingType == ListingType.Rental ? $"{l.Price.Money()}/ay" : l.Price.Money();

    public static string BedsLabel(this Listing l) => l.Bedrooms == 0 ? "Stüdyo" : $"{l.Bedrooms} BR";

    public static string BathsLabel(this Listing l) => $"{l.Bathrooms.ToString("0.#", Us)} BT";

    public static string MainPhoto(this Listing l) =>
        l.Photos.OrderBy(p => p.SortOrder).FirstOrDefault()?.Url ?? "/img/listing-1.svg";

    public static string Excerpt(this string? html, int max = 200)
    {
        if (string.IsNullOrEmpty(html)) return "";
        var text = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ");
        text = System.Net.WebUtility.HtmlDecode(System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ")).Trim();
        return text.Length <= max ? text : text[..max].TrimEnd() + "...";
    }
}
