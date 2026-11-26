using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Html;

namespace Sozluk.Web.Services;

/// <summary>
/// Entry içeriğini güvenli HTML'e çevirir. Önce tüm metin HTML-encode edilir,
/// sonra sözlük işaretlemeleri uygulanır:
///   (bkz: başlık)   -> başlığa link
///   `başlık`        -> hede link (başlığa link, metin aynen)
///   (ara: kelime)   -> arama linki
///   #123            -> entry permalink
///   http(s)://...   -> dış link
/// </summary>
public static partial class EntryFormatter
{
    public static IHtmlContent Format(string content)
    {
        var html = WebUtility.HtmlEncode(content ?? "");

        html = Bkz().Replace(html, m =>
        {
            var target = WebUtility.HtmlDecode(m.Groups[1].Value).Trim().Trim('`').Trim();
            return $"(bkz: {TopicLink(target)})";
        });

        html = Ara().Replace(html, m =>
        {
            var q = WebUtility.HtmlDecode(m.Groups[1].Value).Trim();
            return $"(ara: <a href=\"/ara?q={Uri.EscapeDataString(q)}&amp;mode=search\">{WebUtility.HtmlEncode(q)}</a>)";
        });

        html = Hede().Replace(html, m =>
        {
            var target = WebUtility.HtmlDecode(m.Groups[1].Value).Trim();
            return TopicLink(target);
        });

        html = Url().Replace(html, m =>
        {
            var url = m.Value;
            var display = url.Length > 60 ? url[..57] + "..." : url;
            return $"<a href=\"{url}\" rel=\"nofollow noopener\" target=\"_blank\">{display}</a>";
        });

        html = EntryRef().Replace(html, m =>
            $"{m.Groups[1].Value}<a href=\"/entry/{m.Groups[2].Value}\">#{m.Groups[2].Value}</a>");

        html = html.Replace("\r\n", "\n").Replace("\n", "<br>");
        return new HtmlString(html);
    }

    private static string TopicLink(string title) =>
        $"<a href=\"/ara?q={Uri.EscapeDataString(title)}\">{WebUtility.HtmlEncode(title)}</a>";

    [GeneratedRegex(@"\(bkz:\s*([^()\n]{1,100})\)", RegexOptions.IgnoreCase)]
    private static partial Regex Bkz();

    [GeneratedRegex(@"\(ara:\s*([^()\n]{1,100})\)", RegexOptions.IgnoreCase)]
    private static partial Regex Ara();

    [GeneratedRegex(@"`([^`\n]{1,100})`")]
    private static partial Regex Hede();

    // Encode edilmiş metinde & -> &amp; olur; URL'yi boşluk, < veya " görene kadar al.
    [GeneratedRegex(@"(?<![=""'>])https?://[^\s<""']+")]
    private static partial Regex Url();

    [GeneratedRegex(@"(^|[\s(])#(\d{1,10})\b")]
    private static partial Regex EntryRef();
}
