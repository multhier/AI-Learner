using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebKoy.Web.Data;

namespace WebKoy.Web.Services;

public class SiteFileException(string message) : Exception(message);

public record UploadItem(string FileName, long Length, Func<Stream> Open);

/// <summary>Dosya yöneticisinin iş kuralları: kota, dosya türü, klasör yapısı, meta veri senkronu.</summary>
public class SiteFileService
{
    private readonly AppDbContext _db;
    private readonly IFileStorage _storage;
    private readonly PlatformOptions _opt;

    public SiteFileService(AppDbContext db, IFileStorage storage, IOptions<PlatformOptions> opt)
    {
        _db = db;
        _storage = storage;
        _opt = opt.Value;
    }

    public static string RequirePath(string? input, bool allowRoot = false)
    {
        var p = SitePath.Normalize(input);
        if (p is null) throw new SiteFileException("Geçersiz dosya yolu. Harf, rakam, - _ . ~ ( ) [ ] ve boşluk kullanabilirsiniz.");
        if (!allowRoot && p.Length == 0) throw new SiteFileException("Dosya adı boş olamaz.");
        return p;
    }

    public async Task<List<SiteFile>> ListAsync(int siteId, string dir)
    {
        var prefix = dir.Length == 0 ? "" : dir + "/";
        var items = await _db.SiteFiles.AsNoTracking()
            .Where(f => f.SiteId == siteId && f.Path.StartsWith(prefix))
            .ToListAsync();
        return items
            .Where(f => SitePath.Parent(f.Path) == dir)
            .OrderByDescending(f => f.IsDirectory)
            .ThenBy(f => f.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public Task<SiteFile?> FindAsync(int siteId, string path)
        => _db.SiteFiles.FirstOrDefaultAsync(f => f.SiteId == siteId && f.Path == path);

    public async Task<int> UploadAsync(Site site, string dir, IReadOnlyList<UploadItem> items, CancellationToken ct = default)
    {
        dir = RequirePath(dir, allowRoot: true);
        if (items.Count == 0) throw new SiteFileException("Yüklenecek dosya seçilmedi.");

        // Önce hepsini doğrula: kısmi yükleme yerine ya hep ya hiç.
        var planned = new List<(string Path, UploadItem Item)>();
        foreach (var item in items)
        {
            var path = RequirePath(SitePath.Combine(dir, item.FileName));
            if (!FileTypePolicy.IsAllowed(path))
                throw new SiteFileException($"'{SitePath.FileName(path)}' türündeki dosyalara izin verilmiyor.");
            if (item.Length > _opt.MaxFileBytes)
                throw new SiteFileException($"'{SitePath.FileName(path)}' çok büyük (en fazla {Format.Bytes(_opt.MaxFileBytes)}).");
            planned.Add((path, item));
        }

        var paths = planned.Select(p => p.Path).ToList();
        var existing = await _db.SiteFiles.Where(f => f.SiteId == site.Id && paths.Contains(f.Path)).ToDictionaryAsync(f => f.Path, ct);

        if (existing.Values.Any(f => f.IsDirectory))
            throw new SiteFileException("Aynı adda bir klasör zaten var.");

        var delta = planned.Sum(p => p.Item.Length - (existing.TryGetValue(p.Path, out var e) ? e.Size : 0));
        EnsureQuota(site, delta);

        var newCount = planned.Count(p => !existing.ContainsKey(p.Path));
        await EnsureFileCountAsync(site.Id, newCount);

        foreach (var (path, item) in planned)
        {
            await EnsureParentDirectoriesAsync(site.Id, path);
            await using var stream = item.Open();
            await _storage.WriteAsync(site.Id, path, stream, ct);
            Upsert(site.Id, path, item.Length, existing);
        }

        site.StorageBytes += delta;
        site.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return planned.Count;
    }

    public async Task SaveTextAsync(Site site, string path, string content, bool mustBeNew = false)
    {
        path = RequirePath(path);
        if (!FileTypePolicy.IsEditableText(path))
            throw new SiteFileException("Bu dosya türü metin olarak düzenlenemez.");

        var bytes = Encoding.UTF8.GetBytes(content.Replace("\r\n", "\n"));
        if (bytes.Length > _opt.MaxEditableTextBytes)
            throw new SiteFileException($"Metin dosyası en fazla {Format.Bytes(_opt.MaxEditableTextBytes)} olabilir.");

        var existing = await FindAsync(site.Id, path);
        if (existing is { IsDirectory: true }) throw new SiteFileException("Bu adda bir klasör var.");
        if (existing is not null && mustBeNew) throw new SiteFileException("Bu adda bir dosya zaten var.");

        var delta = bytes.Length - (existing?.Size ?? 0);
        EnsureQuota(site, delta);
        if (existing is null) await EnsureFileCountAsync(site.Id, 1);

        await EnsureParentDirectoriesAsync(site.Id, path);
        await _storage.WriteAsync(site.Id, path, new MemoryStream(bytes));

        if (existing is null)
            _db.SiteFiles.Add(new SiteFile { SiteId = site.Id, Path = path, Size = bytes.Length });
        else
        {
            existing.Size = bytes.Length;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        site.StorageBytes += delta;
        site.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<string> ReadTextAsync(int siteId, string path)
    {
        path = RequirePath(path);
        var file = await FindAsync(siteId, path);
        if (file is null || file.IsDirectory) throw new SiteFileException("Dosya bulunamadı.");
        if (!FileTypePolicy.IsEditableText(path)) throw new SiteFileException("Bu dosya metin olarak düzenlenemez.");
        if (file.Size > _opt.MaxEditableTextBytes) throw new SiteFileException("Dosya düzenleyicide açılamayacak kadar büyük.");

        await using var s = _storage.OpenRead(siteId, path) ?? throw new SiteFileException("Dosya içeriği bulunamadı.");
        using var reader = new StreamReader(s, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    public async Task CreateFolderAsync(Site site, string path)
    {
        path = RequirePath(path);
        if (await FindAsync(site.Id, path) is not null) throw new SiteFileException("Bu adda bir dosya veya klasör zaten var.");
        await EnsureFileCountAsync(site.Id, 1);
        await EnsureParentDirectoriesAsync(site.Id, path);
        _db.SiteFiles.Add(new SiteFile { SiteId = site.Id, Path = path, IsDirectory = true });
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Site site, string path)
    {
        path = RequirePath(path);
        if (path == "index.html") throw new SiteFileException("Ana sayfa (index.html) silinemez; içeriğini düzenleyebilirsiniz.");

        var target = await FindAsync(site.Id, path) ?? throw new SiteFileException("Dosya bulunamadı.");
        var affected = new List<SiteFile> { target };
        if (target.IsDirectory)
        {
            var prefix = path + "/";
            affected.AddRange(await _db.SiteFiles.Where(f => f.SiteId == site.Id && f.Path.StartsWith(prefix)).ToListAsync());
        }

        _db.SiteFiles.RemoveRange(affected);
        site.StorageBytes = Math.Max(0, site.StorageBytes - affected.Sum(f => f.Size));
        site.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        _storage.Delete(site.Id, path);
    }

    public async Task RenameAsync(Site site, string from, string to)
    {
        from = RequirePath(from);
        to = RequirePath(to);
        if (from == to) return;
        if (from == "index.html") throw new SiteFileException("Ana sayfa (index.html) yeniden adlandırılamaz.");
        if (to == from || to.StartsWith(from + "/")) throw new SiteFileException("Klasör kendi içine taşınamaz.");

        var source = await FindAsync(site.Id, from) ?? throw new SiteFileException("Dosya bulunamadı.");
        if (!source.IsDirectory && !FileTypePolicy.IsAllowed(to)) throw new SiteFileException("Bu dosya uzantısına izin verilmiyor.");
        if (await FindAsync(site.Id, to) is not null) throw new SiteFileException("Hedefte aynı adda bir dosya var.");

        await EnsureParentDirectoriesAsync(site.Id, to);
        _storage.Move(site.Id, from, to);

        source.Path = to;
        source.UpdatedAt = DateTime.UtcNow;
        if (source.IsDirectory)
        {
            var prefix = from + "/";
            var children = await _db.SiteFiles.Where(f => f.SiteId == site.Id && f.Path.StartsWith(prefix)).ToListAsync();
            foreach (var c in children) c.Path = to + "/" + c.Path[prefix.Length..];
        }
        site.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    private void EnsureQuota(Site site, long delta)
    {
        if (delta > 0 && site.StorageBytes + delta > _opt.QuotaBytes)
            throw new SiteFileException($"Depolama kotanız doluyor. Kalan: {Format.Bytes(Math.Max(0, _opt.QuotaBytes - site.StorageBytes))}.");
    }

    private async Task EnsureFileCountAsync(int siteId, int adding)
    {
        if (adding <= 0) return;
        var count = await _db.SiteFiles.CountAsync(f => f.SiteId == siteId);
        if (count + adding > _opt.MaxFilesPerSite)
            throw new SiteFileException($"Bir sitede en fazla {_opt.MaxFilesPerSite} dosya olabilir.");
    }

    private async Task EnsureParentDirectoriesAsync(int siteId, string path)
    {
        foreach (var dir in SitePath.Ancestors(path))
        {
            var local = _db.SiteFiles.Local.FirstOrDefault(f => f.SiteId == siteId && f.Path == dir);
            var entry = local ?? await FindAsync(siteId, dir);
            if (entry is null)
                _db.SiteFiles.Add(new SiteFile { SiteId = siteId, Path = dir, IsDirectory = true });
            else if (!entry.IsDirectory)
                throw new SiteFileException($"'{dir}' bir dosya; içine dosya konamaz.");
        }
    }

    private void Upsert(int siteId, string path, long size, Dictionary<string, SiteFile> existing)
    {
        if (existing.TryGetValue(path, out var f))
        {
            f.Size = size;
            f.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            f = new SiteFile { SiteId = siteId, Path = path, Size = size };
            _db.SiteFiles.Add(f);
            existing[path] = f;
        }
    }
}

public static class Format
{
    public static string Bytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double v = bytes;
        var i = 0;
        while (v >= 1024 && i < units.Length - 1) { v /= 1024; i++; }
        return i == 0 ? $"{bytes} B" : $"{v:0.#} {units[i]}";
    }
}
