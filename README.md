# NvhRealty: ASP.NET Core MVC + MSSQL Emlak Sitesi

[nvhny.com](https://www.nvhny.com/) (RealtyMX/ColdFusion tabanlı bir New York emlak sitesi), REA ile tersine
mühendislik yapılarak ASP.NET Core 8 MVC ve Microsoft SQL Server ile yeniden yazıldı.
Analizin ayrıntıları: [docs/reverse-engineering-report.md](docs/reverse-engineering-report.md).

## Özellikler

**Ziyaretçi sitesi**
- Anasayfa: slider, hızlı arama, "Neden bizimle çalışmalısınız?" bölümü, son satılık ve kiralık ilanlar.
- İlanlar: Satılık, Kiralık, Ticari, Açık Ev ve Kısa Dönem listeleri.
  - Orijinal sitedeki tüm filtreler: semt seçici, yatak odası, banyo, fiyat, kaynak, durum, mülk tipi, bina ve daire özellikleri, evcil hayvan.
  - 6 sıralama seçeneği, Liste / Harita / Bölünmüş görünüm (Leaflet + OpenStreetMap), sayfalama.
  - "Uyarı Oluştur" ile aramayı kaydetme.
- İlan detayı: galeri, özellikler, danışman kartı, bilgi formu, benzer ilanlar, favorilere ekleme.
- Danışmanlar: alfabetik gezinme, dil ve isim filtresi, profil (hakkında, ilanlar, yorumlar, iletişim), V-Card indirme.
- Blog (Faydalı İpuçları) ve Haberler (yıl/ay arşivi), Müşteri Yorumları, CMS sayfaları.
- Formlar: İletişim, Mülkünü Listele, Kariyer başvurusu (lisans durumu ile).
- Üyelik: kayıt (kiralık/satılık ilgisi), giriş, favoriler.
- Eski `index.cfm?page=...` adresleri yeni rotalara 301 ile yönlendirilir.

**Yönetim paneli (`/admin`, Admin rolü)**
- Özet ekranı.
- İlan, danışman, blog/haber, yorum ve sayfa yönetimi (CRUD, fotoğraf yükleme).
- Gelen kutusu: mesajlar, kariyer başvuruları, arama uyarıları.
- Site ayarları: şirket adı, adres, telefon, sosyal medya.

## Teknoloji

- .NET 8, ASP.NET Core MVC (Razor), ASP.NET Core Identity
- Entity Framework Core 8 + **SQL Server** (code-first migration)
- Bootstrap 5, jQuery validation, Leaflet
- xUnit + `WebApplicationFactory` entegrasyon testleri

## Kurulum

### 1. Veritabanı

Windows'ta LocalDB hazırdır. Varsayılan bağlantı dizesi `(localdb)\MSSQLLocalDB` kullanır.

Docker ile SQL Server için:

```bash
docker compose up -d
```

Ardından bağlantı dizesini ortam değişkeniyle verin:

```bash
export ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=NvhRealty;User Id=sa;Password=Your_strong_Passw0rd;TrustServerCertificate=True"
```

### 2. Çalıştırma

```bash
dotnet run --project src/NvhRealty.Web
```

İlk açılışta migration'lar otomatik uygulanır ve demo veriler eklenir (`Database:MigrateAndSeedOnStartup`).
Bu veriler: 6 bölge, 189 semt, 5 danışman, 48 ilan, blog yazıları, yorumlar ve 13 CMS sayfası.

- Geliştirme ortamındaki yönetici hesabı `appsettings.Development.json` dosyasında tanımlı: `admin@example.com` / `Admin12345`.
- Canlı ortamda `Admin__Email` ve `Admin__Password` ortam değişkenlerini kullanın.

SQL'i elle uygulamak isterseniz idempotent betik [database/schema.sql](database/schema.sql) dosyasındadır:

```bash
dotnet tool restore
dotnet ef migrations script --idempotent -p src/NvhRealty.Web -o database/schema.sql
```

### 3. Testler

```bash
dotnet test
```

Testler EF InMemory veritabanıyla çalışır (SQL Server gerekmez). Kapsadıkları:
- tüm genel sayfalar,
- arama filtresi,
- eski URL yönlendirmeleri,
- iletişim formunun kaydı,
- yönetici girişi ve panel sayfaları.

## Proje yapısı

```
src/NvhRealty.Web/
  Controllers/          Home, Properties, Agents, Content, Forms, Account, Legacy
  Areas/Admin/          Yönetim paneli (controller + view)
  Data/                 AppDbContext, DbSeeder, Migrations, Seed/neighborhoods.json
  Models/Entities/      Listing, Agent, Neighborhood, Post, Testimonial, ContentPage, ...
  Models/ViewModels/    Arama sorgusu, form modelleri
  Services/             ListingSearchService, SiteSettingsService, FileStorageService
  Views/                Razor görünümleri
tests/NvhRealty.Tests/  Entegrasyon testleri
database/schema.sql     MSSQL şema betiği
docs/                   Tersine mühendislik raporu
```

## Not

Orijinal sitedeki metinler, kişi isimleri, müşteri yorumları ve görseller ilgili şirkete aittir ve projeye
**kopyalanmadı**. Yapı, işlevler, rota ve veri modeli birebir alındı. İçerik ise kurgusal demo verisidir.
Kendi içeriğinizi yönetim panelinden ekleyebilirsiniz.
