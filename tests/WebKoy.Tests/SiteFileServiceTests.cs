using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebKoy.Web.Data;
using WebKoy.Web.Services;

namespace WebKoy.Tests;

public sealed class SiteFileServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "webkoy-tests-" + Guid.NewGuid().ToString("N"));
    private readonly AppDbContext _db;
    private readonly LocalFileStorage _storage;
    private readonly PlatformOptions _opt = new() { QuotaBytes = 100, MaxFileBytes = 60, MaxFilesPerSite = 10 };
    private readonly SiteFileService _svc;
    private readonly Site _site;

    public SiteFileServiceTests()
    {
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        _storage = new LocalFileStorage(_root);
        _svc = new SiteFileService(_db, _storage, Options.Create(_opt));
        var user = new ApplicationUser { UserName = "ayse", Email = "a@example.com" };
        _site = new Site { Name = "ayse", Owner = user };
        _db.Sites.Add(_site);
        _db.SaveChanges();
    }

    public void Dispose()
    {
        _db.Dispose();
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }

    private static UploadItem Item(string name, string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        return new UploadItem(name, bytes.Length, () => new MemoryStream(bytes));
    }

    private string Read(string path)
    {
        using var s = _storage.OpenRead(_site.Id, path)!;
        return new StreamReader(s).ReadToEnd();
    }

    [Fact]
    public async Task Upload_creates_parent_folders_and_tracks_storage()
    {
        await _svc.UploadAsync(_site, "", [Item("img/cats/a.png", "12345")]);

        var paths = await _db.SiteFiles.Select(f => f.Path).OrderBy(p => p).ToListAsync();
        Assert.Equal(new[] { "img", "img/cats", "img/cats/a.png" }, paths);
        Assert.Equal(5, _site.StorageBytes);
        Assert.Equal("12345", Read("img/cats/a.png"));
    }

    [Fact]
    public async Task Overwrite_only_counts_size_difference()
    {
        await _svc.UploadAsync(_site, "", [Item("a.txt", "1234567890")]);
        await _svc.UploadAsync(_site, "", [Item("a.txt", "12")]);
        Assert.Equal(2, _site.StorageBytes);
        Assert.Equal(1, await _db.SiteFiles.CountAsync());
    }

    [Fact]
    public async Task Upload_over_quota_is_rejected_and_writes_nothing()
    {
        await _svc.UploadAsync(_site, "", [Item("a.txt", new string('x', 50))]);
        var ex = await Assert.ThrowsAsync<SiteFileException>(() =>
            _svc.UploadAsync(_site, "", [Item("b.txt", new string('x', 30)), Item("c.txt", new string('x', 30))]));
        Assert.Contains("kota", ex.Message);
        Assert.Null(_storage.OpenRead(_site.Id, "b.txt"));
        Assert.Equal(50, _site.StorageBytes);
    }

    [Fact]
    public async Task Disallowed_type_and_traversal_are_rejected()
    {
        await Assert.ThrowsAsync<SiteFileException>(() => _svc.UploadAsync(_site, "", [Item("x.exe", "1")]));
        await Assert.ThrowsAsync<SiteFileException>(() => _svc.UploadAsync(_site, "", [Item("../x.html", "1")]));
        await Assert.ThrowsAsync<SiteFileException>(() => _svc.UploadAsync(_site, "../..", [Item("x.html", "1")]));
    }

    [Fact]
    public async Task File_too_large_is_rejected()
        => await Assert.ThrowsAsync<SiteFileException>(() => _svc.UploadAsync(_site, "", [Item("a.txt", new string('x', 61))]));

    [Fact]
    public async Task File_count_limit_is_enforced()
    {
        var items = Enumerable.Range(0, 11).Select(i => Item($"f{i}.txt", "1")).ToList();
        await Assert.ThrowsAsync<SiteFileException>(() => _svc.UploadAsync(_site, "", items));
    }

    [Fact]
    public async Task Deleting_folder_removes_children_and_frees_storage()
    {
        await _svc.UploadAsync(_site, "", [Item("blog/a.html", "aaa"), Item("blog/sub/b.html", "bb"), Item("keep.txt", "k")]);
        await _svc.DeleteAsync(_site, "blog");

        Assert.Equal(new[] { "keep.txt" }, await _db.SiteFiles.Select(f => f.Path).ToListAsync());
        Assert.Equal(1, _site.StorageBytes);
        Assert.Null(_storage.OpenRead(_site.Id, "blog/a.html"));
    }

    [Fact]
    public async Task Index_html_cannot_be_deleted_or_renamed()
    {
        await _svc.SaveTextAsync(_site, "index.html", "<h1>hi</h1>");
        await Assert.ThrowsAsync<SiteFileException>(() => _svc.DeleteAsync(_site, "index.html"));
        await Assert.ThrowsAsync<SiteFileException>(() => _svc.RenameAsync(_site, "index.html", "x.html"));
    }

    [Fact]
    public async Task Renaming_folder_moves_children()
    {
        await _svc.UploadAsync(_site, "", [Item("old/a.html", "A")]);
        await _svc.RenameAsync(_site, "old", "new/place");

        var paths = await _db.SiteFiles.Select(f => f.Path).OrderBy(p => p).ToListAsync();
        Assert.Equal(new[] { "new", "new/place", "new/place/a.html" }, paths);
        Assert.Equal("A", Read("new/place/a.html"));
    }

    [Fact]
    public async Task Folder_cannot_be_moved_into_itself()
    {
        await _svc.CreateFolderAsync(_site, "a");
        await Assert.ThrowsAsync<SiteFileException>(() => _svc.RenameAsync(_site, "a", "a/b"));
    }

    [Fact]
    public async Task Cannot_put_file_inside_a_file()
    {
        await _svc.SaveTextAsync(_site, "a.txt", "x");
        await Assert.ThrowsAsync<SiteFileException>(() => _svc.SaveTextAsync(_site, "a.txt/b.txt", "y"));
    }

    [Fact]
    public async Task SaveText_round_trips_and_normalizes_line_endings()
    {
        await _svc.SaveTextAsync(_site, "s.css", "a\r\nb");
        Assert.Equal("a\nb", await _svc.ReadTextAsync(_site.Id, "s.css"));
    }

    [Fact]
    public async Task List_returns_direct_children_folders_first()
    {
        await _svc.UploadAsync(_site, "", [Item("z.txt", "1"), Item("dir/inner.txt", "1")]);
        var root = await _svc.ListAsync(_site.Id, "");
        Assert.Equal(new[] { "dir", "z.txt" }, root.Select(f => f.Path));
    }

    [Fact]
    public void Storage_refuses_paths_outside_site_root()
        => Assert.Throws<InvalidOperationException>(() => _storage.OpenRead(_site.Id, "../../etc/passwd"));
}
