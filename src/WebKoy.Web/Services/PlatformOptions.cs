namespace WebKoy.Web.Services;

public class PlatformOptions
{
    public const string Section = "Platform";

    /// <summary>Panelin (bu uygulamanın) alan adı, ör. "webkoy.com".</summary>
    public string PanelHost { get; set; } = "localhost";

    /// <summary>Kullanıcı sitelerinin kök alan adı. Site "ali" => "ali.{SitesDomain}".
    /// Güvenlik için panelden FARKLI bir kayıtlı alan adı olmalıdır (ör. "webkoy.site").</summary>
    public string SitesDomain { get; set; } = "sites.localhost";

    public string SitesScheme { get; set; } = "https";

    /// <summary>Site URL'lerine eklenecek port (yalnızca geliştirme için, ör. ":5080").</summary>
    public string SitesPortSuffix { get; set; } = "";

    public string StorageRoot { get; set; } = "App_Data/sites";

    public long QuotaBytes { get; set; } = 1L * 1024 * 1024 * 1024;
    public long MaxFileBytes { get; set; } = 100L * 1024 * 1024;
    /// <summary>Tek bir yükleme isteğinin toplam üst sınırı.</summary>
    public long MaxRequestBytes { get; set; } = 100L * 1024 * 1024;
    public int MaxFilesPerSite { get; set; } = 10_000;
    public long MaxEditableTextBytes { get; set; } = 1L * 1024 * 1024;

    public bool RequireConfirmedEmail { get; set; } = false;

    public string[] AdminEmails { get; set; } = Array.Empty<string>();

    public string SiteUrl(string siteName, string? path = null)
        => $"{SitesScheme}://{siteName}.{SitesDomain}{SitesPortSuffix}/{path?.TrimStart('/')}";
}
