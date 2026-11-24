using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Html;

namespace Kivilcim.Services;

public static class TagParser
{
    public const int MaxTagLength = 50;
    public const int MaxTagsPerPost = 10;

    /// <summary>"#kedi, Yaz Tatili ,#kedi" → ["kedi", "yaz tatili"]</summary>
    public static List<string> Parse(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new List<string>();
        }

        return input
            .Split(new[] { ',', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(Normalize)
            .Where(t => t.Length > 0)
            .Distinct()
            .Take(MaxTagsPerPost)
            .ToList();
    }

    public static string Normalize(string tag)
    {
        var cleaned = Regex.Replace(tag.Trim().TrimStart('#').ToLowerInvariant(), @"[^\p{L}\p{N} _-]", "");
        cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();
        return cleaned.Length > MaxTagLength ? cleaned[..MaxTagLength].Trim() : cleaned;
    }
}

public static class BlogNames
{
    public static readonly Regex ValidPattern = new("^[a-z0-9](?:[a-z0-9-]{1,30})[a-z0-9]$", RegexOptions.Compiled);

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

public static class TextFormat
{
    /// <summary>Metni HTML'e güvenli çevirir, satır sonlarını &lt;br&gt; yapar.</summary>
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
        return utc.ToLocalTime().ToString("d MMM yyyy", new System.Globalization.CultureInfo("tr-TR"));
    }

    public static bool IsSafeHttpUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}

public class ImageStore
{
    public const long MaxBytes = 5 * 1024 * 1024;
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    private readonly IWebHostEnvironment _env;

    public ImageStore(IWebHostEnvironment env) => _env = env;

    /// <summary>Görseli wwwroot/uploads altına rastgele adla kaydeder. Hata varsa mesaj döner.</summary>
    public async Task<(string? Path, string? Error)> SaveAsync(IFormFile file, string folder)
    {
        var ext = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
        {
            return (null, "Sadece JPG, PNG, GIF veya WEBP yükleyebilirsiniz.");
        }
        if (file.Length == 0 || file.Length > MaxBytes)
        {
            return (null, "Görsel en fazla 5 MB olabilir.");
        }
        if (!await HasImageSignatureAsync(file))
        {
            return (null, "Dosya geçerli bir görsel değil.");
        }

        var dir = System.IO.Path.Combine(_env.WebRootPath, "uploads", folder);
        Directory.CreateDirectory(dir);
        var name = $"{Guid.NewGuid():N}{ext}";
        await using (var stream = File.Create(System.IO.Path.Combine(dir, name)))
        {
            await file.CopyToAsync(stream);
        }
        return ($"/uploads/{folder}/{name}", null);
    }

    public void Delete(string? webPath)
    {
        if (string.IsNullOrEmpty(webPath) || !webPath.StartsWith("/uploads/"))
        {
            return;
        }
        var full = System.IO.Path.GetFullPath(System.IO.Path.Combine(_env.WebRootPath, webPath.TrimStart('/')));
        var root = System.IO.Path.GetFullPath(System.IO.Path.Combine(_env.WebRootPath, "uploads"));
        if (full.StartsWith(root) && File.Exists(full))
        {
            File.Delete(full);
        }
    }

    private static async Task<bool> HasImageSignatureAsync(IFormFile file)
    {
        var header = new byte[12];
        await using var s = file.OpenReadStream();
        var read = await s.ReadAsync(header);
        if (read < 4) return false;
        bool Starts(params byte[] sig) => header.Take(sig.Length).SequenceEqual(sig);
        return Starts(0xFF, 0xD8, 0xFF)                                   // JPEG
            || Starts(0x89, 0x50, 0x4E, 0x47)                             // PNG
            || Starts(0x47, 0x49, 0x46, 0x38)                             // GIF
            || (read >= 12 && Starts(0x52, 0x49, 0x46, 0x46)
                && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50); // WEBP
    }
}
