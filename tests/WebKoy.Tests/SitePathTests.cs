using WebKoy.Web.Services;

namespace WebKoy.Tests;

public class SitePathTests
{
    [Theory]
    [InlineData("index.html", "index.html")]
    [InlineData("/img//logo.png", "img/logo.png")]
    [InlineData("a\\b\\c.css", "a/b/c.css")]
    [InlineData("", "")]
    [InlineData("  ", "")]
    [InlineData("my file (1).txt", "my file (1).txt")]
    public void Normalize_accepts_safe_paths(string input, string expected)
        => Assert.Equal(expected, SitePath.Normalize(input));

    [Theory]
    [InlineData("../secret")]
    [InlineData("a/../../b")]
    [InlineData("./a")]
    [InlineData(".htaccess")]
    [InlineData("a/.git/config")]
    [InlineData("a/b:c")]
    [InlineData("a/b<c>.html")]
    [InlineData("trailing.")]
    [InlineData("ğüş.html")]
    public void Normalize_rejects_unsafe_paths(string input)
        => Assert.Null(SitePath.Normalize(input));

    [Fact]
    public void Normalize_rejects_too_deep_paths()
        => Assert.Null(SitePath.Normalize(string.Join('/', Enumerable.Repeat("a", 21))));

    [Fact]
    public void Ancestors_lists_parent_directories()
        => Assert.Equal(new[] { "a", "a/b" }, SitePath.Ancestors("a/b/c.txt"));

    [Fact]
    public void ResolveCandidates_tries_html_and_index()
    {
        Assert.Equal(new[] { "index.html" }, SitePath.ResolveCandidates(""));
        Assert.Equal(new[] { "about", "about.html", "about/index.html" }, SitePath.ResolveCandidates("about"));
        Assert.Equal(new[] { "x.css", "x.css/index.html" }, SitePath.ResolveCandidates("x.css"));
    }
}

public class SiteNameRulesTests
{
    [Theory]
    [InlineData("ayse")]
    [InlineData("my-site-42")]
    [InlineData("ab")]
    public void Valid_names(string name) => Assert.True(SiteNameRules.IsValid(name));

    [Theory]
    [InlineData("a")]
    [InlineData("-abc")]
    [InlineData("abc-")]
    [InlineData("a--b")]
    [InlineData("ayşe")]
    [InlineData("a.b")]
    [InlineData("admin")]
    [InlineData("www")]
    [InlineData("abcdefghijklmnopqrstuvwxyz0123456")]
    public void Invalid_names(string name) => Assert.False(SiteNameRules.IsValid(name));

    [Fact]
    public void Names_are_lowercased() => Assert.True(SiteNameRules.IsValid("AYSE"));
}

public class FileTypePolicyTests
{
    [Theory]
    [InlineData("index.html", true)]
    [InlineData("img/a.PNG", true)]
    [InlineData("app.wasm", true)]
    [InlineData("virus.exe", false)]
    [InlineData("shell.php", false)]
    [InlineData("noext", false)]
    public void Allowed(string path, bool expected) => Assert.Equal(expected, FileTypePolicy.IsAllowed(path));

    [Fact]
    public void Images_are_not_editable_text() => Assert.False(FileTypePolicy.IsEditableText("a.png"));
}
