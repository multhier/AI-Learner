using Microsoft.Extensions.Options;
using WebKoy.Web.Data;

namespace WebKoy.Web.Services;

/// <summary>Yeni site açıldığında başlangıç dosyalarını oluşturur.</summary>
public class SiteProvisioner(SiteFileService files, IOptions<PlatformOptions> opt)
{
    public async Task ProvisionAsync(Site site)
    {
        var url = opt.Value.SiteUrl(site.Name);
        await files.SaveTextAsync(site, "index.html", $$"""
            <!DOCTYPE html>
            <html lang="tr">
              <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1">
                <title>{{site.Name}}</title>
                <link href="/style.css" rel="stylesheet">
              </head>
              <body>
                <h1>Merhaba, burası {{site.Name}}!</h1>
                <p>Bu sayfayı panelden düzenleyerek kendi sitenizi oluşturmaya başlayın.</p>
                <p>Adresiniz: <a href="{{url}}">{{url}}</a></p>
              </body>
            </html>
            """);
        await files.SaveTextAsync(site, "style.css", """
            body {
              background: #fdfaf3;
              color: #222;
              font-family: Verdana, Geneva, sans-serif;
              max-width: 720px;
              margin: 2rem auto;
              padding: 0 1rem;
              line-height: 1.6;
            }
            """);
        await files.SaveTextAsync(site, "not_found.html", """
            <!DOCTYPE html>
            <html lang="tr">
              <head><meta charset="UTF-8"><title>Sayfa bulunamadı</title><link href="/style.css" rel="stylesheet"></head>
              <body><h1>404</h1><p>Aradığınız sayfa burada değil. <a href="/">Ana sayfaya dön</a></p></body>
            </html>
            """);
    }
}
