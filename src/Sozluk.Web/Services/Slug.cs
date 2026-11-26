using System.Text;
using System.Text.RegularExpressions;

namespace Sozluk.Web.Services;

public static partial class Slug
{
    /// <summary>Başlıkları sözlük kurallarına göre normalize eder: küçük harf, tek boşluk, kırpılmış.</summary>
    public static string NormalizeTitle(string title)
    {
        var t = (title ?? "").Trim().ToLower(new System.Globalization.CultureInfo("tr-TR"));
        t = Spaces().Replace(t, " ");
        return t.Length > 100 ? t[..100] : t;
    }

    public static string Create(string title)
    {
        var sb = new StringBuilder();
        foreach (var c in NormalizeTitle(title))
        {
            sb.Append(c switch
            {
                'ç' => "c", 'ğ' => "g", 'ı' => "i", 'ö' => "o", 'ş' => "s", 'ü' => "u",
                'â' => "a", 'î' => "i", 'û' => "u",
                _ when char.IsAsciiLetterOrDigit(c) => c.ToString(),
                _ => "-"
            });
        }
        var s = Dashes().Replace(sb.ToString(), "-").Trim('-');
        if (s.Length > 120) s = s[..120].Trim('-');
        return s.Length == 0 ? "baslik" : s;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Spaces();

    [GeneratedRegex(@"-+")]
    private static partial Regex Dashes();
}
