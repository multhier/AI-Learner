using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using WebKoy.Web.Data;

namespace WebKoy.Web.Services;

/// <summary>Site başına bir API anahtarı. Veritabanında yalnızca SHA-256 özeti tutulur;
/// anahtar oluşturulduğu anda bir kez gösterilir.</summary>
public class ApiKeyService(AppDbContext db)
{
    public const string Prefix = "wk_";

    public static string Hash(string key) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key))).ToLowerInvariant();

    public async Task<string> RegenerateAsync(Site site)
    {
        var key = Prefix + Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
        site.ApiKeyHash = Hash(key);
        site.ApiKeyCreatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return key;
    }

    public async Task RevokeAsync(Site site)
    {
        site.ApiKeyHash = null;
        site.ApiKeyCreatedAt = null;
        await db.SaveChangesAsync();
    }

    public async Task<Site?> FindSiteAsync(string? key)
    {
        if (string.IsNullOrWhiteSpace(key) || !key.StartsWith(Prefix) || key.Length > 100) return null;
        var hash = Hash(key.Trim());
        return await db.Sites.FirstOrDefaultAsync(s => s.ApiKeyHash == hash);
    }
}
