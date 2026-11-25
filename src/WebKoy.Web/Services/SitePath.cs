using System.Text.RegularExpressions;

namespace WebKoy.Web.Services;

/// <summary>Kullanıcıdan gelen dosya yollarını güvenli, normalleştirilmiş biçime çevirir.
/// Dizin dışına çıkma (../), mutlak yol, gizli dosya ve tuhaf karakterleri reddeder.</summary>
public static partial class SitePath
{
    public const int MaxLength = 400;
    public const int MaxDepth = 20;

    [GeneratedRegex(@"^[A-Za-z0-9_\-.~ ()\[\]]+$")]
    private static partial Regex SegmentPattern();

    /// <summary>Geçerliyse "a/b/c.html" biçiminde yol döner, kök için "" döner, geçersizse null.</summary>
    public static string? Normalize(string? input)
    {
        if (input is null) return "";
        var s = input.Replace('\\', '/').Trim();
        if (s.Length > MaxLength) return null;

        var parts = s.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > MaxDepth) return null;

        foreach (var p in parts)
        {
            if (p == "." || p == "..") return null;
            if (p.StartsWith('.')) return null;           // .htaccess, .git vb.
            if (p.EndsWith(' ') || p.EndsWith('.')) return null;
            if (!SegmentPattern().IsMatch(p)) return null;
        }
        return string.Join('/', parts);
    }

    public static string Parent(string path)
    {
        var i = path.LastIndexOf('/');
        return i < 0 ? "" : path[..i];
    }

    public static string FileName(string path)
    {
        var i = path.LastIndexOf('/');
        return i < 0 ? path : path[(i + 1)..];
    }

    public static string Combine(string dir, string name)
        => string.IsNullOrEmpty(dir) ? name : $"{dir}/{name}";

    /// <summary>"a/b/c.txt" için ["a", "a/b"] döner.</summary>
    public static IEnumerable<string> Ancestors(string path)
    {
        var parts = path.Split('/');
        for (var i = 1; i < parts.Length; i++)
            yield return string.Join('/', parts[..i]);
    }

    public static string Extension(string path)
    {
        var name = FileName(path);
        var i = name.LastIndexOf('.');
        return i <= 0 ? "" : name[(i + 1)..].ToLowerInvariant();
    }

    /// <summary>Bir istek yolu için sırayla denenecek dosya adayları (ör. /about => about, about.html, about/index.html).</summary>
    public static IEnumerable<string> ResolveCandidates(string normalizedRequestPath)
    {
        if (normalizedRequestPath.Length == 0)
        {
            yield return "index.html";
            yield break;
        }
        yield return normalizedRequestPath;
        if (Extension(normalizedRequestPath) == "")
            yield return normalizedRequestPath + ".html";
        yield return normalizedRequestPath + "/index.html";
    }
}
