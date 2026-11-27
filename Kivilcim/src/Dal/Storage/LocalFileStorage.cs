using Kivilcim.Core.Abstractions;
using Microsoft.Extensions.Options;

namespace Kivilcim.Dal.Storage;

public sealed class StorageOptions
{
    /// <summary>Genel dosyaların kök klasörü (genellikle wwwroot). Web katmanı bunu ayarlar.</summary>
    public string WebRootPath { get; set; } = "wwwroot";
}

/// <summary>Dosyaları wwwroot/uploads altına diske yazar. Yalnızca bu klasör içinde işlem yapar.</summary>
public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(IOptions<StorageOptions> options)
    {
        _root = Path.GetFullPath(options.Value.WebRootPath);
    }

    public async Task<string> SaveAsync(Stream content, string folder, string extension, CancellationToken ct = default)
    {
        var dir = Path.Combine(_root, "uploads", folder);
        Directory.CreateDirectory(dir);
        var name = $"{Guid.NewGuid():N}{extension}";
        await using var target = File.Create(Path.Combine(dir, name));
        await content.CopyToAsync(target, ct);
        return $"/uploads/{folder}/{name}";
    }

    public void Delete(string? publicPath)
    {
        if (string.IsNullOrEmpty(publicPath) || !publicPath.StartsWith("/uploads/", StringComparison.Ordinal))
        {
            return;
        }

        var full = Path.GetFullPath(Path.Combine(_root, publicPath.TrimStart('/')));
        var uploads = Path.GetFullPath(Path.Combine(_root, "uploads")) + Path.DirectorySeparatorChar;
        if (full.StartsWith(uploads, StringComparison.Ordinal) && File.Exists(full))
        {
            File.Delete(full);
        }
    }
}
