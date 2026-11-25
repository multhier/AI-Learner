using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebKoy.Web.Data;

namespace WebKoy.Web.Services;

/// <summary>Kullanıcıya gösterilebilir iş kuralı hatası. <see cref="ErrorType"/> API yanıtındaki error_type alanıdır.</summary>
public class SiteFileException(string message, string errorType = "invalid_request") : Exception(message)
{
    public string ErrorType { get; } = errorType;
}

public record UploadItem(string FileName, long Length, Func<Stream> Open);

public record UploadResult(int Written, int Unchanged)
{
    public int Total => Written + Unchanged;
}

/// <summary>Dosya yöneticisinin iş kuralları: kota, dosya türü, klasör yapısı, meta veri senkronu.
/// Panel ve API aynı kuralları kullanır.</summary>
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
        if (p is null) throw new SiteFileException("Geçersiz dosya yolu. Harf, rakam, - _ . ~ ( ) [ ] ve boşluk kullanabilirsiniz.", "invalid_path");
        if (!allowRoot && p.Length == 0) throw new SiteFileException("Dosya yolu boş olamaz.", "missing_path");
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

    public Task<List<SiteFile>> ListAllAsync(int siteId) =>
        _db.SiteFiles.AsNoTracking().Where(f => f.SiteId == siteId).OrderBy(f => f.Path).ToListAsync();

    public Task<SiteFile?> FindAsync(int siteId, string path)
        => _db.SiteFiles.FirstOrDefaultAsync(f => f.SiteId == siteId && f.Path == path);

    public async Task<UploadResult> UploadAsync(Site site, string dir, IReadOnlyList<UploadItem> items, CancellationToken ct = default)
    {
        dir = RequirePath(dir, allowRoot: true);
        if (items.Count == 0) throw new SiteFileException("Yüklenecek dosya seçilmedi.", "missing_files");

        // Önce hepsini doğrula: kısmi yükleme yerine ya hep ya hiç.
        var planned = new List<(string Path, UploadItem Item)>();
        foreach (var item in items)
        {
            var path = RequirePath(SitePath.Combine(dir, item.FileName));
            if (!FileTypePolicy.IsAllowed(path))
                throw new SiteFileException($"'{SitePath.FileName(path)}' türündeki dosyalara izin verilmiyor.", "invalid_file_type");
            if (item.Length > _opt.MaxFileBytes)
                throw new SiteFileException($"'{SitePath.FileName(path)}' çok büyük (en fazla {Format.Bytes(_opt.MaxFileBytes)}).", "too_large");
            if (planned.Any(p => p.Path == path))
                throw new SiteFileException($"'{path}' aynı istekte iki kez gönderildi.", "duplicate_path");
            planned.Add((path, item));
        }
        if (planned.Sum(p => p.Item.Length) > _opt.MaxRequestBytes)
            throw new SiteFileException($"Bir yükleme isteği en fazla {Format.Bytes(_opt.MaxRequestBytes)} olabilir.", "too_large");

        var paths = planned.Select(p => p.Path).ToList();
        var existing = await _db.SiteFiles.Where(f => f.SiteId == site.Id && paths.Contains(f.Path)).ToDictionaryAsync(f => f.Path, ct);

        if (existing.Values.Any(f => f.IsDirectory))
            throw new SiteFileException("Aynı adda bir klasör zaten var.", "directory_exists");

        var delta = planned.Sum(p => p.Item.Length - (existing.TryGetValue(p.Path, out var e) ? e.Size : 0));
        EnsureQuota(site, delta);
        await EnsureFileCountAsync(site.Id, planned.Count(p => !existing.ContainsKey(p.Path)));

        int written = 0, unchanged = 0;
        foreach (var (path, item) in planned)
        {
            await EnsureParentDirectoriesAsync(site.Id, path);
            await using var stream = item.Open();
            var sha1 = await _storage.WriteAsync(site.Id, path, stream, ct);
            if (existing.TryGetValue(path, out var f) && f.Sha1 == sha1 && f.Size == item.Length)
            {
                unchanged++; // İçerik aynı: güncelleme zamanını ve "son güncellenen" sıralamasını oynatma.
                continue;
            }
            Upsert(site.Id, path, item.Length, sha1, existing);
            written++;
        }

        site.StorageBytes += delta;
        if (written > 0) site.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return new UploadResult(written, unchanged);
    }

    public async Task SaveTextAsync(Site site, string path, string content, bool mustBeNew = false)
    {
        path = RequirePath(path);
        if (!FileTypePolicy.IsEditableText(path))
            throw new SiteFileException("Bu dosya türü metin olarak düzenlenemez.", "invalid_file_type");

        var bytes = Encoding.UTF8.GetBytes(content.Replace("\r\n", "\n"));
        if (bytes.Length > _opt.MaxEditableTextBytes)
            throw new SiteFileException($"Metin dosyası en fazla {Format.Bytes(_opt.MaxEditableTextBytes)} olabilir.", "too_large");

        var existing = await FindAsync(site.Id, path);
        if (existing is { IsDirectory: true }) throw new SiteFileException("Bu adda bir klasör var.", "directory_exists");
        if (existing is not null && mustBeNew) throw new SiteFileException("Bu adda bir dosya zaten var.", "file_exists");

        var delta = bytes.Length - (existing?.Size ?? 0);
        EnsureQuota(site, delta);
        if (existing is null) await EnsureFileCountAsync(site.Id, 1);

        await EnsureParentDirectoriesAsync(site.Id, path);
        var sha1 = await _storage.WriteAsync(site.Id, path, new MemoryStream(bytes));
        if (existing is not null && existing.Sha1 == sha1) return;

        if (existing is null)
            _db.SiteFiles.Add(new SiteFile { SiteId = site.Id, Path = path, Size = bytes.Length, Sha1 = sha1 });
        else
        {
            existing.Size = bytes.Length;
            existing.Sha1 = sha1;
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
        if (file is null || file.IsDirectory) throw new SiteFileException("Dosya bulunamadı.", "missing_file");
        if (!FileTypePolicy.IsEditableText(path)) throw new SiteFileException("Bu dosya metin olarak düzenlenemez.", "invalid_file_type");
        if (file.Size > _opt.MaxEditableTextBytes) throw new SiteFileException("Dosya düzenleyicide açılamayacak kadar büyük.", "too_large");

        await using var s = _storage.OpenRead(siteId, path) ?? throw new SiteFileException("Dosya içeriği bulunamadı.", "missing_file");
        using var reader = new StreamReader(s, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    public async Task CreateFolderAsync(Site site, string path)
    {
        path = RequirePath(path);
        if (await FindAsync(site.Id, path) is not null) throw new SiteFileException("Bu adda bir dosya veya klasör zaten var.", "file_exists");
        if (SitePath.Extension(path) is "html" or "htm") throw new SiteFileException("Klasör adı .html veya .htm ile bitemez.", "invalid_path");
        await EnsureFileCountAsync(site.Id, 1);
        await EnsureParentDirectoriesAsync(site.Id, path);
        _db.SiteFiles.Add(new SiteFile { SiteId = site.Id, Path = path, IsDirectory = true });
        await _db.SaveChangesAsync();
    }

    public Task DeleteAsync(Site site, string path) => DeleteManyAsync(site, [path]);

    /// <summary>Tüm yollar önce doğrulanır; biri geçersizse hiçbir şey silinmez.</summary>
    public async Task DeleteManyAsync(Site site, IReadOnlyList<string> inputPaths)
    {
        if (inputPaths.Count == 0) throw new SiteFileException("Silinecek dosya belirtilmedi.", "missing_filenames");

        var targets = new List<SiteFile>();
        foreach (var input in inputPaths)
        {
            var path = RequirePath(input);
            if (path == "index.html") throw new SiteFileException("Ana sayfa (index.html) silinemez; içeriğini düzenleyebilirsiniz.", "cannot_delete_index");
            var target = await FindAsync(site.Id, path) ?? throw new SiteFileException($"'{path}' bulunamadı.", "missing_file");
            targets.Add(target);
        }

        var affected = new Dictionary<int, SiteFile>();
        foreach (var t in targets)
        {
            affected[t.Id] = t;
            if (!t.IsDirectory) continue;
            var prefix = t.Path + "/";
            foreach (var c in await _db.SiteFiles.Where(f => f.SiteId == site.Id && f.Path.StartsWith(prefix)).ToListAsync())
                affected[c.Id] = c;
        }

        _db.SiteFiles.RemoveRange(affected.Values);
        site.StorageBytes = Math.Max(0, site.StorageBytes - affected.Values.Sum(f => f.Size));
        site.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        foreach (var t in targets) _storage.Delete(site.Id, t.Path);
    }

    /// <summary>Neocities ile aynı kurallar: hedefin üst klasörü var olmalı, üzerine yazılmaz,
    /// index.html taşınmaz, klasör kendi içine taşınmaz ve .html/.htm adı alamaz.</summary>
    public async Task RenameAsync(Site site, string from, string to)
    {
        from = RequirePath(from);
        to = RequirePath(to);
        if (from == to) return;
        if (from == "index.html") throw new SiteFileException("Ana sayfa (index.html) yeniden adlandırılamaz.", "cannot_rename_index");

        var source = await FindAsync(site.Id, from) ?? throw new SiteFileException("Dosya bulunamadı.", "missing_file");
        if (source.IsDirectory)
        {
            if (to.StartsWith(from + "/")) throw new SiteFileException("Klasör kendi içine taşınamaz.", "invalid_path");
            if (SitePath.Extension(to) is "html" or "htm") throw new SiteFileException("Klasör adı .html veya .htm ile bitemez.", "invalid_path");
        }
        else if (!FileTypePolicy.IsAllowed(to))
            throw new SiteFileException("Bu dosya uzantısına izin verilmiyor.", "invalid_file_type");

        if (await FindAsync(site.Id, to) is not null) throw new SiteFileException("Hedefte aynı adda bir dosya var.", "file_exists");
        var parent = SitePath.Parent(to);
        if (parent.Length > 0 && await FindAsync(site.Id, parent) is not { IsDirectory: true })
            throw new SiteFileException($"Hedef klasör '{parent}' yok; önce klasörü oluşturun.", "missing_directory");

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
            throw new SiteFileException($"Depolama kotanız doluyor. Kalan: {Format.Bytes(Math.Max(0, _opt.QuotaBytes - site.StorageBytes))}.", "too_large");
    }

    private async Task EnsureFileCountAsync(int siteId, int adding)
    {
        if (adding <= 0) return;
        var count = await _db.SiteFiles.CountAsync(f => f.SiteId == siteId);
        if (count + adding > _opt.MaxFilesPerSite)
            throw new SiteFileException($"Bir sitede en fazla {_opt.MaxFilesPerSite} dosya olabilir.", "too_many_files");
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
                throw new SiteFileException($"'{dir}' bir dosya; içine dosya konamaz.", "file_exists");
        }
    }

    private void Upsert(int siteId, string path, long size, string sha1, Dictionary<string, SiteFile> existing)
    {
        if (existing.TryGetValue(path, out var f))
        {
            f.Size = size;
            f.Sha1 = sha1;
            f.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            f = new SiteFile { SiteId = siteId, Path = path, Size = size, Sha1 = sha1 };
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

    /// <summary>RFC 2822 tarih biçimi (Neocities API ile aynı): "Tue, 01 Sep 2026 12:00:00 +0000".</summary>
    public static string Rfc2822(DateTime utc) =>
        DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToString("ddd, dd MMM yyyy HH:mm:ss '+0000'", System.Globalization.CultureInfo.InvariantCulture);
}
