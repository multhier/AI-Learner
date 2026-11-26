using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NvhRealty.Web.Data;

namespace NvhRealty.Tests;

/// <summary>Uygulamayı MSSQL yerine EF InMemory veritabanıyla ayağa kaldırır.</summary>
public class AppFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "nvh-tests-" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            var descriptor = services.Single(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            services.Remove(descriptor);
            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(_dbName));
        });
    }
}

public class SiteTests : IClassFixture<AppFactory>
{
    private readonly AppFactory _factory;
    public SiteTests(AppFactory factory) => _factory = factory;

    private HttpClient Client(bool followRedirects = true) =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = followRedirects, HandleCookies = true });

    [Theory]
    [InlineData("/")]
    [InlineData("/properties")]
    [InlineData("/properties/sales")]
    [InlineData("/properties/rentals")]
    [InlineData("/properties/commercial")]
    [InlineData("/properties/open-house")]
    [InlineData("/properties/short-term")]
    [InlineData("/properties/sales?View=map")]
    [InlineData("/properties/rentals?View=split&Sort=price_desc&MinBeds=1&Doorman=true")]
    [InlineData("/properties/1")]
    [InlineData("/agents")]
    [InlineData("/agents?letter=M")]
    [InlineData("/agents/1")]
    [InlineData("/agents/1/vcard")]
    [InlineData("/blog")]
    [InlineData("/blog/cema-nedir")]
    [InlineData("/news")]
    [InlineData("/testimonials")]
    [InlineData("/contact")]
    [InlineData("/careers")]
    [InlineData("/owners")]
    [InlineData("/page/about")]
    [InlineData("/page/disclaimer")]
    [InlineData("/resources/buyers")]
    [InlineData("/account/login")]
    [InlineData("/account/register")]
    public async Task Public_pages_render(string url)
    {
        var res = await Client().GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Unknown_page_returns_404()
    {
        var res = await Client().GetAsync("/page/does-not-exist");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Theory]
    [InlineData("/index.cfm?page=properties&status=1,11,12&isMLS=1&isCompany=1", "/properties/sales")]
    [InlineData("/index.cfm?page=properties&status=2,21&isMLS=1&isCompany=1&ShortTerm=1", "/properties/short-term")]
    [InlineData("/index.cfm?page=properties&status=1,2,11,12,21&isMLS=0&isCompany=1&openHouseChecked=1", "/properties/open-house")]
    [InlineData("/index.cfm?page=content&cms=about", "/page/about")]
    [InlineData("/index.cfm?page=agents&id=15", "/agents/15")]
    [InlineData("/index.cfm?page=resources&sub=renters", "/resources/renters")]
    public async Task Legacy_coldfusion_urls_redirect(string legacy, string expected)
    {
        var res = await Client(followRedirects: false).GetAsync(legacy);
        Assert.Equal(HttpStatusCode.MovedPermanently, res.StatusCode);
        Assert.Equal(expected, res.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Rental_search_only_returns_rentals_with_price_filter()
    {
        var html = await Client().GetStringAsync("/properties/rentals?MaxPrice=4000&PageSize=48");
        var prices = Regex.Matches(html, @"listing-price"">\$([\d,]+)(/ay)?<").Select(m => (decimal.Parse(m.Groups[1].Value.Replace(",", "")), m.Groups[2].Success)).ToList();
        Assert.NotEmpty(prices);
        Assert.All(prices, p => { Assert.True(p.Item1 <= 4000); Assert.True(p.Item2, "Kiralık fiyat /ay ile gösterilmeli"); });
    }

    [Fact]
    public async Task Contact_form_saves_message()
    {
        var client = Client();
        var form = await client.GetStringAsync("/contact");
        var token = Regex.Match(form, @"name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""").Groups[1].Value;
        var res = await client.PostAsync("/contact", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Name"] = "Test Kullanıcı",
            ["Email"] = "test@example.com",
            ["Message"] = "Merhaba, bilgi almak istiyorum."
        }));
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.True(await db.ContactMessages.AnyAsync(m => m.Email == "test@example.com"));
    }

    [Fact]
    public async Task Admin_requires_login()
    {
        var res = await Client(followRedirects: false).GetAsync("/admin/listings");
        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
        Assert.Contains("/account/login", res.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Admin_can_log_in_and_open_panels()
    {
        var client = Client();
        var login = await client.GetStringAsync("/account/login");
        var token = Regex.Match(login, @"name=""__RequestVerificationToken"" type=""hidden"" value=""([^""]+)""").Groups[1].Value;
        var res = await client.PostAsync("/account/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Email"] = "admin@example.com",
            ["Password"] = "Admin12345",
            ["ReturnUrl"] = "/admin"
        }));
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Contains("Özet", await res.Content.ReadAsStringAsync());

        foreach (var url in new[] { "/admin/listings", "/admin/listings/create", "/admin/listings/edit/1", "/admin/agents", "/admin/agents/edit/1",
                                    "/admin/posts", "/admin/posts/create", "/admin/testimonials", "/admin/testimonials/edit/1",
                                    "/admin/pages", "/admin/pages/edit/1", "/admin/messages", "/admin/messages?tab=applications", "/admin/settings" })
        {
            var r = await client.GetAsync(url);
            Assert.True(r.StatusCode == HttpStatusCode.OK, $"{url} → {(int)r.StatusCode}");
        }
    }
}
