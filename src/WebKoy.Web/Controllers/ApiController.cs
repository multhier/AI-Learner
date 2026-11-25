using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using WebKoy.Web.Data;
using WebKoy.Web.Middleware;
using WebKoy.Web.Services;

namespace WebKoy.Web.Controllers;

/// <summary>
/// Geliştirici API'si. Sözleşme Neocities API'si ile uyumludur (docs/neocities-analiz.md):
/// Bearer API anahtarı, GET için query string, POST için form alanları, JSON yanıtlar
/// {"result":"success"|"error", ...}. 400 istek hatası, 403 kimlik hatası, 404 bilinmeyen uç nokta.
/// </summary>
[ApiController, Route("api"), IgnoreAntiforgeryToken, EnableRateLimiting("api")]
public class ApiController(AppDbContext db, ApiKeyService keys, SiteFileService files, IFileStorage storage) : ControllerBase
{
    private Site? _site;

    private async Task<Site?> AuthenticateAsync()
    {
        if (_site is not null) return _site;
        var header = Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return null;
        _site = await keys.FindSiteAsync(header["Bearer ".Length..].Trim());
        return _site is { IsBanned: false } ? _site : null;
    }

    private ObjectResult Error(int status, string type, string message) =>
        StatusCode(status, new { result = "error", error_type = type, message });

    private ObjectResult InvalidAuth() =>
        Error(403, "invalid_auth", "geçersiz kimlik bilgisi: Authorization: Bearer <API anahtarı> başlığını gönderin");

    private async Task<IActionResult> Run(Func<Site, Task<IActionResult>> action)
    {
        var site = await AuthenticateAsync();
        if (site is null) return InvalidAuth();
        try { return await action(site); }
        catch (SiteFileException ex) { return Error(400, ex.ErrorType, ex.Message); }
    }

    [HttpPost("upload")]
    [RequestSizeLimit(110L * 1024 * 1024)]
    public Task<IActionResult> Upload() => Run(async site =>
    {
        if (!Request.HasFormContentType) return Error(400, "missing_files", "multipart/form-data ile en az bir dosya gönderin");
        var form = await Request.ReadFormAsync();
        // Neocities gibi: form alanının adı hedef yoldur.
        var items = form.Files.Select(f => new UploadItem(f.Name, f.Length, f.OpenReadStream)).ToList();
        var result = await files.UploadAsync(site, "", items, HttpContext.RequestAborted);
        var message = result.Written == 0
            ? "dosya(lar) zaten güncel"
            : "dosya(lar) başarıyla yüklendi";
        return Ok(new { result = "success", message });
    });

    [HttpPost("create_directory")]
    public Task<IActionResult> CreateDirectory([FromForm] string? path) => Run(async site =>
    {
        if (string.IsNullOrWhiteSpace(path)) return Error(400, "missing_path", "path alanı gerekli");
        await files.CreateFolderAsync(site, path);
        return Ok(new { result = "success", message = "klasör oluşturuldu" });
    });

    [HttpPost("rename")]
    public Task<IActionResult> Rename([FromForm] string? path, [FromForm(Name = "new_path")] string? newPath) => Run(async site =>
    {
        if (string.IsNullOrWhiteSpace(path)) return Error(400, "missing_path", "path alanı gerekli");
        if (string.IsNullOrWhiteSpace(newPath)) return Error(400, "missing_new_path", "new_path alanı gerekli");
        await files.RenameAsync(site, path, newPath);
        return Ok(new { result = "success", message = $"{SitePath.Normalize(path)} yeni adı: {SitePath.Normalize(newPath)}" });
    });

    [HttpPost("delete")]
    public Task<IActionResult> Delete([FromForm(Name = "filenames[]")] List<string>? filenames) => Run(async site =>
    {
        await files.DeleteManyAsync(site, filenames ?? []);
        return Ok(new { result = "success", message = "dosya(lar) silindi" });
    });

    [HttpGet("list")]
    public Task<IActionResult> List([FromQuery] string? path) => Run(async site =>
    {
        var dir = SiteFileService.RequirePath(path, allowRoot: true);
        var list = dir.Length == 0 ? await files.ListAllAsync(site.Id) : await files.ListAsync(site.Id, dir);
        return Ok(new
        {
            result = "success",
            files = list.Select(f => f.IsDirectory
                ? (object)new { path = f.Path, is_directory = true, created_at = Format.Rfc2822(f.CreatedAt), updated_at = Format.Rfc2822(f.UpdatedAt) }
                : new { path = f.Path, is_directory = false, size = f.Size, created_at = Format.Rfc2822(f.CreatedAt), updated_at = Format.Rfc2822(f.UpdatedAt), sha1_hash = f.Sha1 }),
        });
    });

    [HttpGet("download")]
    public Task<IActionResult> Download([FromQuery] string? path) => Run(async site =>
    {
        if (string.IsNullOrWhiteSpace(path)) return Error(400, "missing_path", "path parametresi gerekli");
        var p = SiteFileService.RequirePath(path);
        var file = await files.FindAsync(site.Id, p);
        if (file is null || file.IsDirectory) return Error(400, "missing_file", "dosya bulunamadı");
        var stream = storage.OpenRead(site.Id, p);
        if (stream is null) return Error(400, "missing_file", "dosya bulunamadı");

        Response.Headers[HeaderNames.CacheControl] = "private, no-store";
        return File(stream, UserSiteMiddleware.ContentTypeFor(p),
            lastModified: new DateTimeOffset(DateTime.SpecifyKind(file.UpdatedAt, DateTimeKind.Utc)),
            entityTag: new EntityTagHeaderValue($"\"{file.Sha1}\""));
    });

    [HttpGet("info")]
    public async Task<IActionResult> Info([FromQuery] string? sitename)
    {
        Site? site;
        if (!string.IsNullOrWhiteSpace(sitename))
        {
            var n = SiteNameRules.Normalize(sitename);
            site = await db.Sites.FirstOrDefaultAsync(s => s.Name == n && !s.IsBanned);
            if (site is null) return Error(400, "missing_site", "site bulunamadı");
        }
        else
        {
            site = await AuthenticateAsync();
            if (site is null) return InvalidAuth();
        }

        var tags = await db.SiteTags.Where(t => t.SiteId == site.Id).Select(t => t.Tag.Name).ToListAsync();
        var updated = site.UpdatedAt > site.CreatedAt ? Format.Rfc2822(site.UpdatedAt) : null;
        return Ok(new
        {
            result = "success",
            info = new
            {
                sitename = site.Name,
                views = site.Views,
                hits = site.Hits,
                created_at = Format.Rfc2822(site.CreatedAt),
                last_updated = updated,
                domain = (string?)null,
                supporter = false,
                tags,
            },
        });
    }

    [HttpPost("upload_hash")]
    public Task<IActionResult> UploadHash() => Run(async site =>
    {
        if (!Request.HasFormContentType) return Error(400, "missing_files", "yol=sha1 biçiminde form alanları gönderin");
        var form = await Request.ReadFormAsync();
        var result = new Dictionary<string, bool>();
        var lookup = (await files.ListAllAsync(site.Id)).Where(f => !f.IsDirectory).ToDictionary(f => f.Path, f => f.Sha1);
        foreach (var (rawPath, values) in form)
        {
            if (values.Count != 1) return Error(400, "invalid_hash", $"'{rawPath}' için tek bir değer gönderin");
            var p = SitePath.Normalize(rawPath);
            result[rawPath] = p is not null && lookup.TryGetValue(p, out var sha1)
                && string.Equals(sha1, values[0]?.Trim(), StringComparison.OrdinalIgnoreCase);
        }
        return Ok(new { result = "success", files = result });
    });

    [Route("{*rest}")]
    public IActionResult NotFoundEndpoint() => Error(404, "not_found", "bilinmeyen API uç noktası");
}
