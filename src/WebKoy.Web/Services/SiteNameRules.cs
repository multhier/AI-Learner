using System.Text.RegularExpressions;

namespace WebKoy.Web.Services;

/// <summary>Site adı = alt alan adı. DNS etiket kurallarına ve rezerve isimlere uyar.</summary>
public static partial class SiteNameRules
{
    public const int MinLength = 2;
    public const int MaxLength = 32;

    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "www", "api", "admin", "administrator", "root", "mail", "email", "smtp", "imap", "pop", "ftp",
        "blog", "help", "support", "status", "static", "cdn", "assets", "dashboard", "panel", "login",
        "register", "signup", "account", "settings", "security", "abuse", "webmaster", "hostmaster",
        "postmaster", "dev", "test", "staging", "ns1", "ns2", "official", "staff", "moderator"
    };

    [GeneratedRegex("^[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$")]
    private static partial Regex Pattern();

    public static string Normalize(string name) => (name ?? "").Trim().ToLowerInvariant();

    public static string? Validate(string name)
    {
        var n = Normalize(name);
        if (n.Length < MinLength || n.Length > MaxLength)
            return $"Site adı {MinLength}-{MaxLength} karakter olmalı.";
        if (!Pattern().IsMatch(n))
            return "Site adı yalnızca küçük harf, rakam ve tire içerebilir; tire ile başlayıp bitemez.";
        if (n.Contains("--"))
            return "Site adında art arda iki tire olamaz.";
        if (Reserved.Contains(n))
            return "Bu site adı ayrılmış, başka bir ad seçin.";
        return null;
    }

    public static bool IsValid(string name) => Validate(name) is null;
}
