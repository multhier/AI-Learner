# WebKöy

Neocities tarzı, ücretsiz kişisel web sitesi barındırma platformu. **ASP.NET Core 8 MVC**, **Entity Framework Core** ve **Microsoft SQL Server** ile yazıldı.

Kullanıcılar kaydolur, `kullanici.sitealanadi.com` adresinde bir site alır, HTML/CSS/JS dosyalarını tarayıcıdan yükleyip düzenler. Diğer siteleri keşfedip takip edebilir, beğenebilir ve yorum bırakabilir.

## Özellikler

| Alan | İçerik |
|---|---|
| Hesap | Kayıt (site adı = kullanıcı adı, ilgi alanı etiketleri, hCaptcha), giriş, e-posta doğrulama, şifre sıfırlama, hesap kilitleme, kalıcı hesap silme |
| Dosya yöneticisi | Çoklu dosya ve klasör yükleme, sürükle-bırak, klasör oluşturma, yeniden adlandırma ve taşıma, silme, kota çubuğu |
| Düzenleyici | Tarayıcıda kod düzenleme (CodeMirror), Ctrl+S ile AJAX kaydetme, kaydedilmemiş değişiklik uyarısı |
| Yayınlama | `{site}.{SitesDomain}` adresinden sunum, `/about` → `about.html` / `about/index.html`, özel `not_found.html`, ETag/304 |
| Topluluk | Keşfet sayfası (7 sıralama, etiket, arama), site profili, takip, beğeni, yorum, görüntülenme (IP başına günde 1) ve istek (hits) sayacı |
| Geliştirici API'si | Site başına API anahtarı; `upload`, `create_directory`, `rename`, `delete`, `list`, `download`, `info`, `upload_hash`. Neocities API'siyle aynı sözleşme ([/developers](src/WebKoy.Web/Views/Home/Developers.cshtml)) |
| Moderasyon | Şikayet etme, yönetici paneli, siteyi askıya alma (HTTP 451) |
| Güvenlik | Ayrı alan adında kullanıcı içeriği, CSRF koruması, rate limiting, yol doğrulama (path traversal), dosya türü beyaz listesi, kota ve dosya sayısı sınırı |

## Neocities analizi

Neocities'in dışarıdan gözlenebilen davranışı [REA](https://github.com/morluto/rea) ile kayıt altına alındı. Bulgular ve WebKöy ile karşılaştırması: [docs/neocities-analiz.md](docs/neocities-analiz.md). Analiz yalnızca herkese açık sayfaları kapsar; Neocities'in kaynak kodu kullanılmadı.

## Mimari

```
                 ┌────────────── webkoy.com (Panel: MVC + Identity çerezi)
 Tarayıcı ──► Kestrel ─┤
                 └────────────── *.webkoy.site (UserSiteMiddleware: yalnızca GET/HEAD, çerez yok)
                                         │
              MSSQL (kullanıcı, site, dosya meta verisi, sosyal veri)
              IFileStorage (dosya içerikleri: yerel disk; S3/Azure Blob'a geçirilebilir)
```

**Neden iki ayrı alan adı?** Kullanıcılar kendi JavaScript'lerini yayınlıyor. Panel ile kullanıcı siteleri aynı kayıtlı alan adında olursa, kötü niyetli bir site panel oturumlarına saldırabilir. Neocities ve GitHub Pages bu yüzden ayrı alan adı kullanır (`github.io`). Kullanıcı sitesi alan adını ayrıca [Public Suffix List](https://publicsuffix.org/submit/)'e eklemeniz önerilir. Böylece siteler birbirinin çerezlerini de okuyamaz.

```
src/WebKoy.Web
├── Controllers/      Home (ana sayfa, keşfet), Account, Dashboard (dosya yöneticisi), Sites (profil, sosyal), Admin
├── Data/             Entity'ler, AppDbContext, Migrations, DbInitializer (migration + admin rolü)
├── Middleware/       UserSiteMiddleware: alt alan adından statik dosya sunumu
├── Services/         SiteFileService (kota/tür/klasör kuralları), SitePath (güvenli yol), SiteNameRules,
│                     FileTypePolicy, LocalFileStorage, SocialService, ViewCounter, EmailSender
└── Views/            Razor görünümleri (Türkçe arayüz)
tests/WebKoy.Tests    xUnit: yol güvenliği, ad kuralları, kota, klasör işlemleri, etiketler, API sözleşmesi (WebApplicationFactory)
```

### Veritabanı tabloları

`AspNetUsers` (Identity) · `Sites` (Name benzersiz, kullanıcı başına bir site) · `SiteFiles` (SiteId+Path benzersiz) · `Tags` / `SiteTags` · `Follows` · `Likes` · `Comments` · `Reports`

## Yerelde çalıştırma

Gereksinimler: .NET 8 SDK ve SQL Server (Docker ya da yerel kurulum).

```bash
# 1) SQL Server
docker run -d --name wk-sql -e ACCEPT_EULA=Y -e 'MSSQL_SA_PASSWORD=WebKoy_Dev_123!' -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest

# 2) Uygulama (migration'lar açılışta otomatik uygulanır)
cd src/WebKoy.Web
dotnet run
```

- Panel: <http://localhost:5080>
- Kullanıcı siteleri: `http://<siteadi>.sites.localhost:5080`. Chrome, Edge ve Firefox `*.localhost` adreslerini otomatik olarak 127.0.0.1'e yönlendirir.
- `admin@example.com` ile kayıt olan kişi yönetici olur (`appsettings.Development.json` → `Platform:AdminEmails`).

Tümünü Docker ile çalıştırmak için: `docker compose up --build` → <http://localhost:8080>

Testler: `dotnet test`

Yeni migration: `dotnet ef migrations add <Ad> -p src/WebKoy.Web -o Data/Migrations`

## Yayına alma kontrol listesi

1. **İki alan adı:** `webkoy.com` (panel) ve `webkoy.site` (kullanıcı siteleri).
2. **DNS:** `webkoy.com` ve `*.webkoy.site` için A/AAAA kayıtları.
3. **TLS:** `*.webkoy.site` için wildcard sertifika (Let's Encrypt DNS-01 doğrulaması; Caddy veya Traefik bunu otomatik yapar).
4. **Ters vekil sunucu** (Nginx, Caddy veya YARP): her iki alan adını da uygulamaya yönlendirin. `ForwardedHeaders` yapılandırmasında yalnızca kendi vekil sunucunuzun IP'sine güvenin (`Program.cs`).
5. **Ayarlar** (ortam değişkeni veya gizli anahtar yöneticisi ile):
   - `ConnectionStrings__Default`
   - `Platform__PanelHost=webkoy.com`, `Platform__SitesDomain=webkoy.site`, `Platform__SitesScheme=https`
   - `Platform__StorageRoot=/data/sites` (kalıcı disk) ve `Platform__AdminEmails__0=siz@...`
   - `Smtp__Host`, `Smtp__User`, `Smtp__Password`, `Smtp__From` (e-posta doğrulaması ve şifre sıfırlama için gerekli)
   - `Captcha__SiteKey`, `Captcha__Secret` ([hCaptcha](https://www.hcaptcha.com/) anahtarları; boş bırakılırsa CAPTCHA devre dışı kalır)
6. **Yedekleme:** MSSQL yedekleri ve `StorageRoot` klasörü birlikte yedeklenmeli.
7. **Kullanım koşulları ve KVKK metni:** `Views/Home/Terms.cshtml` bir şablondur, hukukçuya gözden geçirtin.
8. Birden fazla sunucuya ölçeklenirken:
   - `LocalFileStorage` yerine S3 veya Azure Blob uygulaması yazın (`IFileStorage`).
   - Data Protection anahtarlarını ortak bir yerde saklayın (`AddDataProtection().PersistKeysTo...`).
   - Bellek içi önbellek ve sayaçlar yerine Redis kullanın.

## Yol haritası (henüz yok)

- Etkinlik akışı (site güncellemeleri, takipler) ve profil duvarında beğeniler (Neocities'te var, bkz. analiz).
- Bahşiş (tip) ve ücretli destekçi planı.
- Site ekran görüntüleri (keşfet sayfası için Playwright ile arka plan işi).
- Özel alan adı bağlama (CNAME doğrulaması ve otomatik TLS).
- WebDAV veya SFTP ile dosya yükleme, ZIP olarak indirme, komut satırı aracı (API hazır).
- Site aktivite akışı ("takip ettiğin siteler güncellendi").
- Ücretli plan (daha fazla kota ve özel alan adı).
