using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NvhRealty.Web.Data;

namespace NvhRealty.Web.Services;

/// <summary>Şirket adı, adres, telefon gibi ayarları veritabanından okur ve önbellekler.</summary>
public class SiteSettingsService
{
    private const string CacheKey = "site-settings";
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;

    public static readonly IReadOnlyDictionary<string, string> Defaults = new Dictionary<string, string>
    {
        ["CompanyName"] = "Horizon Realty (Demo)",
        ["Slogan"] = "Sizin, Ailenizin, Dostlarınızın... Hayatınızın Yanındayız!",
        ["Address"] = "100 Example Street, Suite 10",
        ["City"] = "New York, NY 10001",
        ["Phone"] = "(212) 555-0199",
        ["Email"] = "info@example.com",
        ["Facebook"] = "",
        ["Twitter"] = "",
        ["LinkedIn"] = "",
        ["HomeListingsText"] = "New York City, Miami ve Güneybatı Florida'da kiralık ve satılık ilanları olan, tam hizmet veren butik bir emlak firmasıyız.",
        ["HomeToolsText"] = "Kullanıcı dostu sitemiz, ihtiyaçlarınıza uyan mülkleri tek tıkla bulmanızı sağlar."
    };

    public SiteSettingsService(AppDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<IReadOnlyDictionary<string, string>> GetAllAsync()
    {
        return (await _cache.GetOrCreateAsync(CacheKey, async e =>
        {
            e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            var fromDb = await _db.SiteSettings.AsNoTracking().ToDictionaryAsync(s => s.Key, s => s.Value ?? "");
            var merged = new Dictionary<string, string>(Defaults);
            foreach (var (k, v) in fromDb) merged[k] = v;
            return (IReadOnlyDictionary<string, string>)merged;
        }))!;
    }

    public async Task<string> GetAsync(string key) =>
        (await GetAllAsync()).TryGetValue(key, out var v) ? v : "";

    public void Invalidate() => _cache.Remove(CacheKey);
}
