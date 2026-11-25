using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using WebKoy.Web.Data;

namespace WebKoy.Web.Services;

/// <summary>Her dosya isteğinde veritabanına yazmamak için hit'leri bellekte biriktirir
/// ve birkaç saniyede bir toplu olarak yazar.</summary>
public class HitCounter(IServiceScopeFactory scopes, ILogger<HitCounter> logger) : BackgroundService
{
    private readonly ConcurrentDictionary<int, long> _pending = new();

    public void Record(int siteId) => _pending.AddOrUpdate(siteId, 1, (_, n) => n + 1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken)) await FlushAsync();
        }
        catch (OperationCanceledException) { }
        await FlushAsync(); // kapanırken kalanları yaz
    }

    public async Task FlushAsync()
    {
        if (_pending.IsEmpty) return;
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        foreach (var siteId in _pending.Keys.ToList())
        {
            if (!_pending.TryRemove(siteId, out var n) || n == 0) continue;
            try
            {
                await db.Sites.Where(s => s.Id == siteId).ExecuteUpdateAsync(s => s.SetProperty(x => x.Hits, x => x.Hits + n));
            }
            catch (Exception ex)
            {
                _pending.AddOrUpdate(siteId, n, (_, m) => m + n);
                logger.LogWarning(ex, "Hit sayacı yazılamadı (site {SiteId}).", siteId);
            }
        }
    }
}
