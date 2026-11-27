using System.Globalization;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace Kivilcim.Web.Helpers;

/// <summary>Görünüm yardımcıları: güvenli metin ve göreli zaman. Sunumla ilgili olduğu için Web'de.</summary>
public static class TextFormat
{
    /// <summary>Metni HTML'e güvenli çevirir (kaçışlı), satır sonlarını &lt;br&gt; yapar.</summary>
    public static IHtmlContent Paragraphs(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return HtmlString.Empty;
        }
        var encoded = HtmlEncoder.Default.Encode(text.Replace("\r\n", "\n"));
        return new HtmlString(encoded.Replace("&#xA;", "<br>"));
    }

    public static string TimeAgo(DateTime utc)
    {
        var span = DateTime.UtcNow - utc;
        if (span.TotalMinutes < 1) return "az önce";
        if (span.TotalHours < 1) return $"{(int)span.TotalMinutes} dk önce";
        if (span.TotalDays < 1) return $"{(int)span.TotalHours} sa önce";
        if (span.TotalDays < 30) return $"{(int)span.TotalDays} gün önce";
        return utc.ToLocalTime().ToString("d MMM yyyy", new CultureInfo("tr-TR"));
    }
}
