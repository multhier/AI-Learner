# Sözlük — ASP.NET Core MVC + MSSQL

İnci Sözlük tarzı, başlık/entry tabanlı bir kolektif sözlük uygulaması.
ASP.NET Core 8 MVC, Entity Framework Core 8 ve Microsoft SQL Server ile yazıldı.

## Özellikler

| Özellik | Adres |
|---|---|
| Gündem / bugün / dün (sol çerçeve) | `/?sol=gundem\|bugun\|dun` |
| Başlık sayfası, sayfalama, şükela/bugün/yazar filtresi | `/baslik/{slug}--{id}?a=nice&p=2` |
| Başlığa git / arama / başlık açma | `/ara?q=...` (`#123` entry'ye, `@nick` yazara gider) |
| Entry permalink, düzenle, sil | `/entry/{id}` |
| Artı / eksi oy, favori, şikayet (AJAX) | `POST /entry/{id}/oy\|favori\|sikayet` |
| Dünün en beğenilenleri, rastgele entry | `/dunun-en-begenilenleri`, `/rastgele` |
| Yazar profili (entry'ler, favoriler, en beğenilenler) | `/yazar/{nick}` |
| Özel mesajlar | `/mesaj`, `/mesaj/{nick}` |
| Kayıt / giriş (çerez tabanlı, PBKDF2 şifre) | `/kayit`, `/giris` |
| Moderasyon: şikayetler, çaylak terfisi, uzaklaştırma, başlık kilitleme | `/mod` |

Entry işaretlemeleri: `(bkz: başlık)`, `` `hede` ``, `(ara: kelime)`, `#entryno`, otomatik URL linkleri.
Tüm içerik önce HTML-encode edilir, sonra işaretlemeler uygulanır (XSS'e karşı).

Roller: **çaylak** (yeni kayıt) → **yazar** (moderatör terfi ettirir) → **moderatör** → **admin**.

## Çalıştırma

### Docker ile (SQL Server dahil)
```bash
docker compose up --build
# http://localhost:8080  — admin / Admin123!
```

### Yerel SQL Server ile
1. `src/Sozluk.Web/appsettings.json` içindeki `ConnectionStrings:DefaultConnection` değerini düzenle.
2. `dotnet run --project src/Sozluk.Web`

Uygulama açılışta EF Core migration'larını otomatik uygular ve örnek veri ekler.
Veritabanını elle kurmak istersen: `database/SozlukDb.sql` (idempotent script).

Varsayılan hesaplar: `admin` / `Admin123!` (admin), `ilk yazar` / `Yazar123!` (yazar).
**Canlıya almadan önce şifreleri değiştir** (`Seed:AdminPassword`).

### SQL Server olmadan hızlı deneme (SQLite)
```bash
Database__Provider=Sqlite dotnet run --project src/Sozluk.Web
```

## Proje yapısı
```
src/Sozluk.Web/
  Controllers/   Home, Topic, Entry, Account, User, Message, Moderation
  Data/          SozlukDbContext, DbSeeder, Migrations (SQL Server)
  Models/        Entities (User, Topic, Entry, Vote, Favorite, Message, Report), ViewModels
  Services/      EntryFormatter (bkz/hede), Slug, EntryQueries
  ViewComponents/GundemViewComponent (sol çerçeve)
  Views/         Razor görünümleri
database/SozlukDb.sql   MSSQL şema script'i
```

## Tersine mühendislik notu
REA (`npx rea-agents setup`) kuruldu ve site incelenmeye çalışıldı; ancak incisozluk.co
Cloudflare bot doğrulamasının arkasında olduğu için otomatik tarayıcıyla sayfalar alınamadı
(koruma atlatılmaya çalışılmadı). Bu yüzden uygulama sitenin kaynak kodundan değil, sözlük
türünün bilinen davranışlarından (başlık/entry modeli, gündem, bkz, oylama, çaylaklık, mesaj)
sıfırdan yazılmıştır; site adı, logosu ve içeriği kullanılmamıştır.
