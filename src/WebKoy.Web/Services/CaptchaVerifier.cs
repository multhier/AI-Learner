using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace WebKoy.Web.Services;

public class CaptchaOptions
{
    public const string Section = "Captcha";
    /// <summary>hCaptcha site anahtarı. Boşsa CAPTCHA devre dışıdır (yerel geliştirme).</summary>
    public string? SiteKey { get; set; }
    public string? Secret { get; set; }
    public string VerifyUrl { get; set; } = "https://api.hcaptcha.com/siteverify";
    public bool Enabled => !string.IsNullOrWhiteSpace(SiteKey) && !string.IsNullOrWhiteSpace(Secret);
}

/// <summary>Kayıt formunda hCaptcha doğrulaması (Neocities kayıtta hCaptcha kullanıyor).</summary>
public class CaptchaVerifier(HttpClient http, IOptions<CaptchaOptions> options, ILogger<CaptchaVerifier> logger)
{
    public bool Enabled => options.Value.Enabled;
    public string? SiteKey => options.Value.SiteKey;

    public async Task<bool> VerifyAsync(string? token, string? remoteIp)
    {
        var o = options.Value;
        if (!o.Enabled) return true;
        if (string.IsNullOrWhiteSpace(token)) return false;

        var form = new Dictionary<string, string> { ["secret"] = o.Secret!, ["response"] = token, ["sitekey"] = o.SiteKey! };
        if (remoteIp is not null) form["remoteip"] = remoteIp;
        try
        {
            using var res = await http.PostAsync(o.VerifyUrl, new FormUrlEncodedContent(form));
            var body = await res.Content.ReadFromJsonAsync<VerifyResponse>();
            return body?.Success == true;
        }
        catch (Exception ex)
        {
            // Doğrulama servisine ulaşılamıyorsa kaydı reddet (spam'e kapı açmamak için).
            logger.LogError(ex, "hCaptcha doğrulaması başarısız.");
            return false;
        }
    }

    private sealed record VerifyResponse([property: JsonPropertyName("success")] bool Success);
}
