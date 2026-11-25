using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WebKoy.Web.Data;

namespace WebKoy.Web.Services;

/// <summary>Aynı ziyaretçinin (IP) bir siteyi günde bir kez saydırmasına izin verir. IP'nin kendisi saklanmaz.</summary>
public class ViewCounter(IMemoryCache cache)
{
    private static readonly byte[] Salt = RandomNumberGenerator.GetBytes(16);

    public async Task CountAsync(AppDbContext db, int siteId, HttpContext ctx)
    {
        var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var hash = Convert.ToHexString(SHA256.HashData(Salt.Concat(Encoding.UTF8.GetBytes($"{siteId}|{ip}")).ToArray()))[..24];
        var key = "view:" + hash;
        if (cache.TryGetValue(key, out _)) return;
        cache.Set(key, true, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1), Size = 1 });

        await db.Sites.Where(s => s.Id == siteId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Views, x => x.Views + 1));
    }
}
