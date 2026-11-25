using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace WebKoy.Web.Services;

/// <summary>Dosya içeriklerinin saklandığı yer. Ölçeklenmek için S3/Azure Blob uygulaması yazılabilir.</summary>
public interface IFileStorage
{
    /// <summary>İçeriği yazar ve SHA-1 özetini (küçük harf hex) döner.</summary>
    Task<string> WriteAsync(int siteId, string path, Stream content, CancellationToken ct = default);
    Stream? OpenRead(int siteId, string path);
    void Delete(int siteId, string path);
    void Move(int siteId, string from, string to);
    void DeleteSite(int siteId);
}

public class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(IOptions<PlatformOptions> options, IWebHostEnvironment env)
        : this(Path.IsPathRooted(options.Value.StorageRoot)
            ? options.Value.StorageRoot
            : Path.Combine(env.ContentRootPath, options.Value.StorageRoot)) { }

    public LocalFileStorage(string root)
    {
        _root = Path.GetFullPath(root);
        Directory.CreateDirectory(_root);
    }

    private string SiteRoot(int siteId) => Path.Combine(_root, siteId.ToString());

    /// <summary>Yol SitePath ile doğrulanmış olsa da, ikinci bir savunma hattı olarak kök dışına çıkışı engeller.</summary>
    private string Physical(int siteId, string path)
    {
        var siteRoot = SiteRoot(siteId);
        var full = Path.GetFullPath(Path.Combine(siteRoot, path.Replace('/', Path.DirectorySeparatorChar)));
        if (!full.StartsWith(siteRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidOperationException("Geçersiz yol.");
        return full;
    }

    public async Task<string> WriteAsync(int siteId, string path, Stream content, CancellationToken ct = default)
    {
        var full = Physical(siteId, path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        var tmp = full + ".upload-" + Guid.NewGuid().ToString("N");
        using var sha1 = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
        try
        {
            await using (var fs = new FileStream(tmp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
            {
                var buffer = new byte[81920];
                int read;
                while ((read = await content.ReadAsync(buffer, ct)) > 0)
                {
                    sha1.AppendData(buffer, 0, read);
                    await fs.WriteAsync(buffer.AsMemory(0, read), ct);
                }
            }
            File.Move(tmp, full, overwrite: true);
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
        }
        return Convert.ToHexString(sha1.GetHashAndReset()).ToLowerInvariant();
    }

    public Stream? OpenRead(int siteId, string path)
    {
        var full = Physical(siteId, path);
        return File.Exists(full)
            ? new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true)
            : null;
    }

    public void Delete(int siteId, string path)
    {
        var full = Physical(siteId, path);
        if (File.Exists(full)) File.Delete(full);
        else if (Directory.Exists(full)) Directory.Delete(full, recursive: true);
    }

    public void Move(int siteId, string from, string to)
    {
        var src = Physical(siteId, from);
        var dst = Physical(siteId, to);
        Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
        if (File.Exists(src)) File.Move(src, dst);
        else if (Directory.Exists(src)) Directory.Move(src, dst);
        else Directory.CreateDirectory(dst); // boş klasör yalnızca veritabanında olabilir
    }

    public void DeleteSite(int siteId)
    {
        var dir = SiteRoot(siteId);
        if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
    }
}
