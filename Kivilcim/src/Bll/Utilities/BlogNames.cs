using System.Text;
using System.Text.RegularExpressions;

namespace Kivilcim.Bll.Utilities;

public static class BlogNames
{
    public static readonly Regex ValidPattern = new("^[a-z0-9](?:[a-z0-9-]{1,30})[a-z0-9]$", RegexOptions.Compiled);

    /// <summary>E-postadan ya da serbest metinden URL'ye uygun blog adı üretir ("Çağla Şen" → "cagla-sen").</summary>
    public static string Slugify(string input)
    {
        var map = new Dictionary<char, char>
        {
            ['ç'] = 'c', ['ğ'] = 'g', ['ı'] = 'i', ['ö'] = 'o', ['ş'] = 's', ['ü'] = 'u',
            ['Ç'] = 'c', ['Ğ'] = 'g', ['İ'] = 'i', ['Ö'] = 'o', ['Ş'] = 's', ['Ü'] = 'u'
        };
        var sb = new StringBuilder();
        foreach (var ch in input)
        {
            var c = map.TryGetValue(ch, out var m) ? m : char.ToLowerInvariant(ch);
            sb.Append(c is >= 'a' and <= 'z' or >= '0' and <= '9' ? c : '-');
        }

        var slug = Regex.Replace(sb.ToString(), "-+", "-").Trim('-');
        if (slug.Length < 3)
        {
            slug = (slug + "-blog").Trim('-');
        }
        return slug.Length > 26 ? slug[..26].Trim('-') : slug;
    }
}
