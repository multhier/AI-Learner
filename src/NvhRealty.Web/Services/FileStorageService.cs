namespace NvhRealty.Web.Services;

/// <summary>Yönetim panelinden yüklenen görselleri wwwroot/uploads altına kaydeder.</summary>
public class FileStorageService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    private const long MaxBytes = 8 * 1024 * 1024;
    private readonly IWebHostEnvironment _env;

    public FileStorageService(IWebHostEnvironment env) => _env = env;

    public async Task<string?> SaveImageAsync(IFormFile? file, string folder)
    {
        if (file is null || file.Length == 0) return null;
        var ext = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(ext)) throw new InvalidOperationException("Yalnızca JPG, PNG, WEBP veya GIF yükleyebilirsiniz.");
        if (file.Length > MaxBytes) throw new InvalidOperationException("Dosya 8 MB'tan büyük olamaz.");

        var safeFolder = string.Concat(folder.Where(char.IsLetterOrDigit));
        var dir = Path.Combine(_env.WebRootPath, "uploads", safeFolder);
        Directory.CreateDirectory(dir);
        var name = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        await using (var stream = File.Create(Path.Combine(dir, name)))
            await file.CopyToAsync(stream);
        return $"/uploads/{safeFolder}/{name}";
    }
}
