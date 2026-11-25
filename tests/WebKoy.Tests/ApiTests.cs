using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebKoy.Web.Data;
using WebKoy.Web.Services;

namespace WebKoy.Tests;

/// <summary>API sözleşmesini (docs/neocities-analiz.md) uygulamanın gerçek HTTP pipeline'ı üzerinden doğrular.</summary>
public sealed class ApiTests : IClassFixture<ApiTests.Factory>
{
    public sealed class Factory : WebApplicationFactory<Program>
    {
        public readonly string Storage = Path.Combine(Path.GetTempPath(), "webkoy-api-" + Guid.NewGuid().ToString("N"));
        private readonly string _dbName = Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("Platform:StorageRoot", Storage);
            builder.UseSetting("Database:MigrateOnStartup", "false");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(_dbName));
            });
        }

        public async Task<(string Key, int SiteId)> CreateSiteAsync(string name)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var site = new Site { Name = name, Owner = new ApplicationUser { UserName = name, Email = $"{name}@example.com" } };
            db.Sites.Add(site);
            await db.SaveChangesAsync();
            await scope.ServiceProvider.GetRequiredService<SiteFileService>().SaveTextAsync(site, "index.html", "<h1>hi</h1>");
            var key = await scope.ServiceProvider.GetRequiredService<ApiKeyService>().RegenerateAsync(site);
            return (key, site.Id);
        }
    }

    private readonly Factory _f;
    public ApiTests(Factory f) => _f = f;

    private async Task<HttpClient> ClientAsync(string site)
    {
        var (key, _) = await _f.CreateSiteAsync(site);
        var c = _f.CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);
        return c;
    }

    private static async Task<JsonElement> Json(HttpResponseMessage r) =>
        JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement;

    private static MultipartFormDataContent Files(params (string Path, string Content)[] files)
    {
        var m = new MultipartFormDataContent();
        foreach (var (p, c) in files) m.Add(new StringContent(c), p, Path.GetFileName(p));
        return m;
    }

    [Fact]
    public async Task Missing_or_wrong_key_returns_403()
    {
        var c = _f.CreateClient();
        var r = await c.GetAsync("/api/list");
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
        Assert.Equal("invalid_auth", (await Json(r)).GetProperty("error_type").GetString());

        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "wk_wrong");
        Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync("/api/list")).StatusCode);
    }

    [Fact]
    public async Task Unknown_endpoint_returns_404_json()
    {
        var r = await _f.CreateClient().GetAsync("/api/nope");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
        Assert.Equal("error", (await Json(r)).GetProperty("result").GetString());
    }

    [Fact]
    public async Task Upload_list_download_roundtrip()
    {
        var c = await ClientAsync("roundtrip");
        var up = await c.PostAsync("/api/upload", Files(("css/style.css", "body{}"), ("about.html", "<p>a</p>")));
        Assert.Equal(HttpStatusCode.OK, up.StatusCode);
        Assert.Equal("success", (await Json(up)).GetProperty("result").GetString());

        var again = await Json(await c.PostAsync("/api/upload", Files(("about.html", "<p>a</p>"))));
        Assert.Contains("güncel", again.GetProperty("message").GetString());

        var list = await Json(await c.GetAsync("/api/list"));
        var paths = list.GetProperty("files").EnumerateArray().Select(f => f.GetProperty("path").GetString()).ToList();
        Assert.Equal(new[] { "about.html", "css", "css/style.css", "index.html" }, paths);
        var css = list.GetProperty("files").EnumerateArray().Single(f => f.GetProperty("path").GetString() == "css/style.css");
        Assert.Equal(6, css.GetProperty("size").GetInt64());
        Assert.Matches("^[0-9a-f]{40}$", css.GetProperty("sha1_hash").GetString());
        Assert.EndsWith("+0000", css.GetProperty("updated_at").GetString());
        var dir = list.GetProperty("files").EnumerateArray().Single(f => f.GetProperty("path").GetString() == "css");
        Assert.False(dir.TryGetProperty("size", out _));

        var sub = await Json(await c.GetAsync("/api/list?path=css"));
        Assert.Single(sub.GetProperty("files").EnumerateArray());

        var dl = await c.GetAsync("/api/download?path=css/style.css");
        Assert.Equal("body{}", await dl.Content.ReadAsStringAsync());
        Assert.True(dl.Headers.CacheControl!.Private && dl.Headers.CacheControl.NoStore);
        Assert.Equal($"\"{css.GetProperty("sha1_hash").GetString()}\"", dl.Headers.ETag!.Tag);
    }

    [Fact]
    public async Task Upload_rejects_bad_type_with_error_type()
    {
        var c = await ClientAsync("badtype");
        var r = await c.PostAsync("/api/upload", Files(("ok.html", "x"), ("virus.exe", "x")));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal("invalid_file_type", (await Json(r)).GetProperty("error_type").GetString());
        var list = await Json(await c.GetAsync("/api/list"));
        Assert.DoesNotContain(list.GetProperty("files").EnumerateArray(), f => f.GetProperty("path").GetString() == "ok.html");
    }

    [Fact]
    public async Task Directory_rename_delete_and_hash()
    {
        var c = await ClientAsync("ops");
        Assert.Equal(HttpStatusCode.OK, (await c.PostAsync("/api/create_directory", new FormUrlEncodedContent([new("path", "a/b")]))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsync("/api/create_directory", new FormUrlEncodedContent([new("path", "a/b")]))).StatusCode);

        await c.PostAsync("/api/upload", Files(("draft.html", "d")));
        var rn = await c.PostAsync("/api/rename", new FormUrlEncodedContent([new("path", "draft.html"), new("new_path", "a/b/final.html")]));
        Assert.Equal(HttpStatusCode.OK, rn.StatusCode);

        var idx = await c.PostAsync("/api/delete", new FormUrlEncodedContent([new("filenames[]", "index.html")]));
        Assert.Equal("cannot_delete_index", (await Json(idx)).GetProperty("error_type").GetString());

        var hash = await Json(await c.PostAsync("/api/upload_hash", new FormUrlEncodedContent([
            new("a/b/final.html", "3c363836cf4e16666669a25da280a1865c2d2874"), // sha1("d")
            new("index.html", "0000000000000000000000000000000000000000"),
            new("missing.html", "x")])));
        Assert.True(hash.GetProperty("files").GetProperty("a/b/final.html").GetBoolean());
        Assert.False(hash.GetProperty("files").GetProperty("index.html").GetBoolean());
        Assert.False(hash.GetProperty("files").GetProperty("missing.html").GetBoolean());

        var del = await c.PostAsync("/api/delete", new FormUrlEncodedContent([new("filenames[]", "a")]));
        Assert.Equal(HttpStatusCode.OK, del.StatusCode);
        var list = await Json(await c.GetAsync("/api/list"));
        Assert.Equal(new[] { "index.html" }, list.GetProperty("files").EnumerateArray().Select(f => f.GetProperty("path").GetString()));
    }

    [Fact]
    public async Task Info_is_public_by_sitename_and_private_without()
    {
        var c = await ClientAsync("infosite");
        var pub = await Json(await _f.CreateClient().GetAsync("/api/info?sitename=infosite"));
        Assert.Equal("infosite", pub.GetProperty("info").GetProperty("sitename").GetString());
        Assert.Equal(JsonValueKind.Null, pub.GetProperty("info").GetProperty("domain").ValueKind);

        var own = await Json(await c.GetAsync("/api/info"));
        Assert.Equal("infosite", own.GetProperty("info").GetProperty("sitename").GetString());

        Assert.Equal(HttpStatusCode.Forbidden, (await _f.CreateClient().GetAsync("/api/info")).StatusCode);
    }

    [Fact]
    public async Task Api_does_not_require_antiforgery_but_panel_does()
    {
        var c = await ClientAsync("csrf");
        Assert.Equal(HttpStatusCode.OK, (await c.PostAsync("/api/create_directory", new FormUrlEncodedContent([new("path", "x")]))).StatusCode);
        Assert.NotEqual(HttpStatusCode.OK, (await _f.CreateClient(new() { AllowAutoRedirect = false })
            .PostAsync("/dashboard/newfolder", new FormUrlEncodedContent([new("name", "x")]))).StatusCode);
    }
}

internal static class ServiceCollectionExtensions
{
    public static void RemoveAll<T>(this IServiceCollection services)
    {
        foreach (var d in services.Where(d => d.ServiceType == typeof(T)).ToList()) services.Remove(d);
    }
}
