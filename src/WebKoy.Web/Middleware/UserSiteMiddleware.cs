using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using WebKoy.Web.Data;
using WebKoy.Web.Services;

namespace WebKoy.Web.Middleware;

/// <summary>
/// "{site}.{SitesDomain}" adresine gelen istekleri yakalar ve kullanıcı dosyalarını sunar.
/// Panel uygulamasının geri kalanı bu alan adında hiç çalışmaz; böylece kullanıcı JavaScript'i
/// panelin çerezlerine/oturumuna erişemez.
/// </summary>
public class UserSiteMiddleware(RequestDelegate next, IOptions<PlatformOptions> options, IMemoryCache cache, IFileStorage storage)
{
    private static readonly FileExtensionContentTypeProvider ContentTypes = CreateProvider();
    private readonly PlatformOptions _opt = options.Value;

    private sealed record SiteInfo(int Id, string Name, bool IsBanned);

    public async Task InvokeAsync(HttpContext ctx, AppDbContext db, ViewCounter views)
    {
        var host = ctx.Request.Host.Host.ToLowerInvariant();
        var suffix = "." + _opt.SitesDomain.ToLowerInvariant();
        if (!host.EndsWith(suffix, StringComparison.Ordinal))
        {
            await next(ctx);
            return;
        }

        ctx.Response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";
        ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        if (!HttpMethods.IsGet(ctx.Request.Method) && !HttpMethods.IsHead(ctx.Request.Method))
        {
            ctx.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
            return;
        }

        var name = host[..^suffix.Length];
        var site = SiteNameRules.IsValid(name) ? await GetSiteAsync(db, name) : null;
        if (site is null)
        {
            await PlainAsync(ctx, 404, "Böyle bir site yok.");
            return;
        }
        if (site.IsBanned)
        {
            await PlainAsync(ctx, 451, "Bu site kullanım koşullarını ihlal ettiği için askıya alındı.");
            return;
        }

        var requestPath = SitePath.Normalize(Uri.UnescapeDataString(ctx.Request.Path.Value ?? "/"));
        if (requestPath is null)
        {
            await ServeNotFoundAsync(ctx, db, site);
            return;
        }

        var candidates = SitePath.ResolveCandidates(requestPath).ToList();
        var matches = await db.SiteFiles.AsNoTracking()
            .Where(f => f.SiteId == site.Id && !f.IsDirectory && candidates.Contains(f.Path))
            .ToListAsync();
        var file = candidates.Select(c => matches.FirstOrDefault(m => m.Path == c)).FirstOrDefault(m => m is not null);

        if (file is null)
        {
            await ServeNotFoundAsync(ctx, db, site);
            return;
        }

        // "/blog" -> "blog/index.html" ise göreli bağlantılar bozulmasın diye "/blog/" adresine yönlendir.
        if (requestPath.Length > 0 && file.Path == requestPath + "/index.html" && !(ctx.Request.Path.Value ?? "").EndsWith('/'))
        {
            ctx.Response.Redirect(ctx.Request.PathBase + ctx.Request.Path + "/" + ctx.Request.QueryString, permanent: true);
            return;
        }

        if (await ServeFileAsync(ctx, site, file, 200) && IsHtml(file.Path))
            await views.CountAsync(db, site.Id, ctx);
    }

    private async Task<SiteInfo?> GetSiteAsync(AppDbContext db, string name)
    {
        var key = "usersite:" + name;
        if (cache.TryGetValue(key, out SiteInfo? info)) return info;
        info = await db.Sites.AsNoTracking().Where(s => s.Name == name)
            .Select(s => new SiteInfo(s.Id, s.Name, s.IsBanned)).FirstOrDefaultAsync();
        cache.Set(key, info, TimeSpan.FromSeconds(30));
        return info;
    }

    public static void Invalidate(IMemoryCache cache, string name) => cache.Remove("usersite:" + name);

    private async Task ServeNotFoundAsync(HttpContext ctx, AppDbContext db, SiteInfo site)
    {
        var nf = await db.SiteFiles.AsNoTracking().FirstOrDefaultAsync(f => f.SiteId == site.Id && f.Path == "not_found.html");
        if (nf is null || !await ServeFileAsync(ctx, site, nf, 404, allowConditional: false))
            await PlainAsync(ctx, 404, "Sayfa bulunamadı.");
    }

    private async Task<bool> ServeFileAsync(HttpContext ctx, SiteInfo site, SiteFile file, int status, bool allowConditional = true)
    {
        var stream = storage.OpenRead(site.Id, file.Path);
        if (stream is null) return false;
        await using var _ = stream;

        var etag = new EntityTagHeaderValue($"\"{file.Size:x}-{file.UpdatedAt.Ticks:x}\"");
        var headers = ctx.Response.GetTypedHeaders();
        headers.ETag = etag;
        headers.LastModified = new DateTimeOffset(DateTime.SpecifyKind(file.UpdatedAt, DateTimeKind.Utc));
        headers.CacheControl = new CacheControlHeaderValue { Public = true, MaxAge = TimeSpan.FromMinutes(1) };

        if (allowConditional && ctx.Request.GetTypedHeaders().IfNoneMatch.Any(t => t.Compare(etag, useStrongComparison: false)))
        {
            ctx.Response.StatusCode = StatusCodes.Status304NotModified;
            return true;
        }

        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = ContentTypeFor(file.Path);
        ctx.Response.ContentLength = stream.Length;
        if (!HttpMethods.IsHead(ctx.Request.Method))
            await stream.CopyToAsync(ctx.Response.Body, ctx.RequestAborted);
        return true;
    }

    public static string ContentTypeFor(string path)
    {
        if (!ContentTypes.TryGetContentType(path, out var type)) type = "application/octet-stream";
        if (FileTypePolicy.IsEditableText(path) && !type.Contains("charset")) type += "; charset=utf-8";
        return type;
    }

    private static bool IsHtml(string path) => SitePath.Extension(path) is "html" or "htm";

    private static async Task PlainAsync(HttpContext ctx, int status, string message)
    {
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "text/plain; charset=utf-8";
        await ctx.Response.WriteAsync(message);
    }

    private static FileExtensionContentTypeProvider CreateProvider()
    {
        var p = new FileExtensionContentTypeProvider();
        p.Mappings[".mjs"] = "text/javascript";
        p.Mappings[".webmanifest"] = "application/manifest+json";
        p.Mappings[".md"] = "text/plain";
        p.Mappings[".wasm"] = "application/wasm";
        p.Mappings[".glb"] = "model/gltf-binary";
        p.Mappings[".gltf"] = "model/gltf+json";
        p.Mappings[".avif"] = "image/avif";
        p.Mappings[".opus"] = "audio/ogg";
        return p;
    }
}
