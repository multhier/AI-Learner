# Kıvılcım Projesi: Adım Adım Ne Yaptım?

Bu dosya, **Kıvılcım** adlı Tumblr benzeri blog sitesini nasıl yaptığımı **baştan sona, sırasıyla** anlatır.
Her adımda önce **ne yaptığımı**, sonra **ne yazdığımı**, en sonda da **bunun ne demek olduğunu** bulacaksın.

> **Kısaca Kıvılcım:** Kullanıcıların blog açıp yazı, fotoğraf, video, alıntı ve bağlantı paylaştığı;
> birbirini **takip ettiği**, **beğendiği**, **mesajlaştığı** bir sosyal sitedir.
> **ASP.NET Core 8 MVC** ve **Microsoft SQL Server** ile yazıldı.

---

## 0. Hazırlık: Önce Tumblr'ı İnceledim

- **Önce** Claude'a tersine mühendislik aracı **REA**'yı kurdum.
  - **REA** = Bir uygulamayı ya da web sitesini **kodunu çalıştırmadan, okuyarak** inceleyen araçtır.
- **Sonra** REA ile **tumblr.com** ana sayfasını inceledim. Şunları öğrendim:
  - Site **React** ile yazılmış.
  - Gönderiler sayfa sayfa yükleniyor ve bunun için **`max_post_id`** diye bir değer kullanılıyor.
    - **Bu şu demektir:** "Bana şu numaradan **küçük** olan 10 gönderiyi getir." Kıvılcım'da da aynı mantığı kullandım.
  - Kullanıcılar istemedikleri etiketleri gizleyebiliyor (**`filtered_tags`**).
- **Önemli:** Tumblr'ın **adını, logosunu ve tasarımını kopyalamadım.** Sadece nasıl çalıştığını öğrendim, siteyi kendi adı ve görünümüyle **sıfırdan** yazdım.

---

## 1. Projeyi Oluşturdum

- **Önce** bilgisayara **.NET 8 SDK**'yı kurdum.
- **Sonra** şu komutla hazır bir MVC projesi oluşturdum:

```bash
dotnet new mvc --auth Individual -o Kivilcim
```

- **Bu şu demektir:**
  - **`mvc`** = Model–View–Controller yapısında bir web projesi.
  - **`--auth Individual`** = Projeye **kayıt ol / giriş yap** sistemi (**ASP.NET Core Identity**) hazır gelsin.
- **Ardından** veritabanı için **NuGet paketlerini** ekledim:
  - **`Microsoft.EntityFrameworkCore.SqlServer`** → SQL Server ile konuşmak için.
  - **`Microsoft.EntityFrameworkCore.Tools`** → **migration** komutları için.

> **MVC nedir?**
> - **Model** = Verinin kendisi (kullanıcı, gönderi...).
> - **View** = Kullanıcının gördüğü sayfa (HTML).
> - **Controller** = İsteği karşılayan, veriyi hazırlayıp sayfaya gönderen ara katman.

---

## 2. Model Katmanını Yazdım (Tablolar)

- **Önce** sitede hangi bilgileri saklayacağımı düşündüm.
- **Sonra** her biri için bir **C# sınıfı** yazdım: `Models/Entities.cs`

| Sınıf | Ne saklar? |
|---|---|
| **ApplicationUser** | Kullanıcı: blog adı, görünen ad, hakkımda, profil fotoğrafı |
| **Post** | Gönderi: türü, başlığı, metni, görseli, videosu, bağlantısı |
| **Tag** / **PostTag** | Etiketler ve hangi gönderide hangi etiket olduğu |
| **Like** | Kim hangi gönderiyi beğendi |
| **Follow** | Kim kimi takip ediyor |
| **Reply** | Gönderiye yazılan yanıtlar |
| **HiddenTag** | Kullanıcının akışında görmek istemediği etiketler |
| **Notification** | Bildirimler ("X gönderini beğendi") |
| **Message** | Özel mesajlar |

- **Bu şu demektir:** **Her sınıf = veritabanında bir tablo**, **her özellik = tabloda bir sütun.**
- Örnek:

```csharp
public class Like
{
    public string UserId { get; set; }   // Beğenen kişi
    public int PostId { get; set; }      // Beğenilen gönderi
    public DateTime CreatedAt { get; set; }
}
```

- **Reblog (yeniden paylaşım) için** `Post` sınıfına **`ReblogOfId`** alanı ekledim.
  - **Bu şu demektir:** Bir gönderi başka bir gönderinin paylaşımıysa, asıl gönderinin numarasını tutar.
  - **Önemli kural:** Reblog'un reblog'u yapılsa bile **her zaman asıl (ilk) gönderiye** bağlanır. Böylece zincir uzamaz.

---

## 3. Data Katmanını Yazdım (Context)

- **Önce** `Data/ApplicationDbContext.cs` dosyasını yazdım.
  - **Context** = Veritabanına **SELECT, INSERT, UPDATE, DELETE** gönderen sınıftır. **İş mantığı içermez.**
- **Sonra** `OnModelCreating` içinde tablolar arasındaki **ilişkileri** tanımladım.
  - Örnek: "Bir **beğeni** bir **gönderiye** aittir; gönderi silinirse beğenisi de silinsin."

### Karşılaştığım SQL Server Sorunu

- **Sorun:** SQL Server, bir tabloya giden **birden fazla otomatik silme (cascade) yolunu** kabul etmez.
  - **Örnek:** Kullanıcı silinince → gönderileri silinir → gönderinin beğenileri silinir.
    Ama aynı anda **kullanıcı → beğeni** yolu da var. **İki yol = hata.**
- **Çözüm:** Bazı ilişkileri **`NoAction`** yaptım ve silme işini **kendim elle** yaptım (`SaveChangesAsync` içinde).
  - **Bu şu demektir:** Kullanıcı silinirken önce onun beğenilerini, mesajlarını, takiplerini ben siliyorum, sonra kullanıcıyı.

### Migration

- **Sonra** şu komutla **migration** oluşturdum:

```bash
dotnet ef migrations add InitialCreate
```

- **Bu şu demektir:** **Migration** = C# sınıflarına bakıp **"veritabanında şu tabloları oluştur"** diyen SQL komutlarının dosyasıdır.
- Site açılırken migration'lar **otomatik** uygulanır, yani veritabanını **elle oluşturmana gerek yok.**

### Örnek Veriler

- **Ardından** `Data/SeedData.cs` dosyasını yazdım.
  - **Bu şu demektir:** Veritabanı boşsa **3 örnek kullanıcı** (Ayşe, Mehmet, Zeynep), **8 gönderi**, takip ve beğeniler eklenir. Böylece site ilk açıldığında boş görünmez.

---

## 4. Servis Katmanını Yazdım

- **Önce** birkaç controller'ın **aynı sorguyu** tekrar tekrar yazdığını fark ettim.
- **Sonra** ortak işleri `Services/` klasörüne topladım:

| Servis | Görevi |
|---|---|
| **PostQueryService** | Akış, blog, etiket ve arama sorguları; **sayfalama**; gizli etiket filtresi |
| **NotificationService** | Bildirimi kaydeder ve kullanıcıya **anında** gönderir |
| **ImageStore** | Görsel ve video yükler, **sahte dosyaları** reddeder |
| **SmtpEmailSender** | Şifre sıfırlama **e-postası** gönderir |
| **TagParser** | `#Kedi, yaz tatili` → **`kedi`**, **`yaz tatili`** |

- **Bu şu demektir:** **Servis** = Controller'ların ortak kullandığı iş kodudur. Aynı kodu **bir kez** yazıp her yerde kullanırım.

---

## 5. Controller Katmanını Yazdım

- **Önce** sitenin hangi adresleri olacağını belirledim.
- **Sonra** her bölüm için bir **controller** yazdım:

| Controller | Adres | Ne yapar? |
|---|---|---|
| **HomeController** | `/` | Giriş yaptıysan **akışı**, yapmadıysan **popüler gönderileri** gösterir |
| **ExploreController** | `/explore`, `/tagged/kedi`, `/search?q=...` | Keşfet, etiket ve arama |
| **PostsController** | `/new/Photo`, `/post/5` | Gönderi oluştur, göster, beğen, reblog, yanıt, sil |
| **BlogController** | `/blog/ayse` | Blog sayfası, takip et / bırak |
| **SettingsController** | `/settings` | Profil, avatar, gizli etiketler |
| **NotificationsController** | `/notifications` | Bildirimler |
| **MessagesController** | `/messages/zeynep` | Mesajlaşma |

- Örnek: Beğeni işlemi

```csharp
[Authorize]                  // Sadece giriş yapmış kullanıcı
[HttpPost]                   // Sadece form gönderimiyle
[ValidateAntiForgeryToken]   // Sahte istekleri engelle
public async Task<IActionResult> Like(int id) { ... }
```

- **Bu şu demektir:**
  - **`[Authorize]`** → Giriş yapmayan beğenemez.
  - **`[HttpPost]`** → Adres çubuğuna yazarak beğeni yapılamaz.
  - **`[ValidateAntiForgeryToken]`** → Başka bir siteden gizlice beğeni gönderilemez (**CSRF koruması**).

---

## 6. View Katmanını Yazdım (Arayüz)

- **Önce** sitenin ortak çerçevesini yazdım: `Views/Shared/_Layout.cshtml`
  - Üstte logo, arama kutusu, menü, 🔔 bildirim ve ✉ mesaj sayaçları.
- **Sonra** her gönderi için **tek bir kart** yazdım: `_PostCard.cshtml`
  - **Bu şu demektir:** Gönderi akışta, blogda, aramada **aynı kartla** gösterilir. Bir yeri değiştirince her yer değişir.
- **Ardından** `wwwroot/js/site.js` dosyasını yazdım:
  - **Beğeni** butonuna basınca **sayfa yenilenmeden** kalp dolar.
  - **"Daha fazla göster"** ile sonraki 10 gönderi aynı sayfaya eklenir.
- Tasarım için **Bootstrap 5** kullandım; site **telefonda da** düzgün görünür.

---

## 7. İlk Testleri Yaptım ve Hataları Düzelttim

- **Önce** gerçek bir tarayıcıyla (**Playwright**) siteyi **bir kullanıcı gibi** kullanan testler yazdım.
- **Sonra** testlerde şu **hataları** buldum ve düzelttim:
  1. **Popüler etiketler** sorgusu SQL'e çevrilemiyordu → sorguyu sadeleştirdim.
  2. **Gizlenen etiketli** bir gönderinin **reblog'u** akışta görünmeye devam ediyordu → reblog'un asıl gönderisinin etiketlerini de kontrol ettim.
  3. **Etiket renkleri** çok soluktu → CSS'i düzelttim.

---

## 8. Eksikleri Tamamladım

### 8.1 Bildirimler (SignalR)

- **Önce** `Hubs/NotificationHub.cs` dosyasını yazdım.
  - **SignalR** = Sunucunun tarayıcıya **sayfa yenilenmeden** mesaj göndermesini sağlayan teknolojidir.
- **Sonra** beğeni, reblog, yanıt ve takip olduğunda bildirim gönderdim.
- **Bu şu demektir:**

> **Zeynep** ♡'ye basar → **PostsController** → **NotificationService** kaydeder → **SignalR** → **Ayşe'nin** ekranında anında balon çıkar ve 🔔 sayacı artar.

- **Kurallar:**
  - Kendi gönderini beğenirsen **bildirim gitmez.**
  - Beğeniyi geri alırsan okunmamış bildirim **silinir.**

### 8.2 Mesajlaşma

- **Önce** `Message` tablosunu ekledim.
- **Sonra** `/messages` (konuşmalar) ve `/messages/zeynep` (sohbet) sayfalarını yazdım.
- Mesaj da **SignalR** ile **anında** karşı tarafa ulaşır.
  - Sohbet açıksa → mesaj baloncuk olarak eklenir.
  - Sohbet kapalıysa → ✉ sayacı artar ve köşede bildirim çıkar.

### 8.3 Video Gönderisi

- **Önce** `Post` tablosuna **`VideoPath`** sütunu ekledim.
- **Sonra** video yüklemeyi yazdım: **MP4** ve **WEBM**, en fazla **50 MB**.
- **Güvenlik:** Dosyanın sadece uzantısına değil, **ilk baytlarına (imzasına)** da bakıyorum.
  - **Bu şu demektir:** Biri `virus.exe` dosyasının adını `video.mp4` yapsa bile **reddedilir.**

### 8.4 Türkçe Giriş ve Kayıt Sayfaları

- **Önce** ASP.NET Identity'nin İngilizce sayfalarının **yerine** kendi Türkçe sayfalarımı yazdım:
  - **Giriş**, **Kayıt**, **Çıkış**, **Şifre değiştir**, **Hesabı sil**.
- **Sonra** hata mesajlarını Türkçeleştirdim: `TurkishIdentityErrorDescriber.cs`
  - Örnek: *"Passwords must have at least one digit"* → **"Şifrede en az bir rakam (0-9) olmalı."**

### 8.5 Şifremi Unuttum

- **Önce** e-posta gönderen servisi yazdım: `SmtpEmailSender.cs`
- **Sonra** **Şifremi unuttum** ve **Yeni şifre belirle** sayfalarını yazdım.
- **Akış:**
  1. Kullanıcı e-postasını yazar.
  2. E-postasına **tek kullanımlık** bir bağlantı gelir.
  3. Bağlantıya tıklayıp **yeni şifre** belirler.
- **Güvenlik:**
  - Bağlantı **bir gün** geçerli ve **bir kez** kullanılabilir.
  - Kayıtlı olmayan bir e-posta yazılsa da **aynı mesaj** gösterilir.
    - **Bu şu demektir:** Kötü niyetli biri hangi e-postaların sitede kayıtlı olduğunu **öğrenemez.**
- **SMTP ayarı yoksa** e-posta gönderilmez, `App_Data/mail` klasörüne **dosya olarak** kaydedilir. Böylece geliştirirken de test edebilirsin.

---

## 9. Gerçek SQL Server'da Test Ettim

- **Önce** test ortamına **SQL Server 2022** kurdum.
- **Sonra** tüm testleri **gerçek SQL Server** üzerinde çalıştırdım.
- **Ayrıca** eski sürümle oluşturulmuş bir veritabanına **yeni migration'ın** sorunsuz uygulandığını denedim.
  - **Bu şu demektir:** Siteyi güncellediğinde **eski veriler kaybolmaz.**

### Bulduğum İnatçı Hata

- **Sorun:** Bir formda hata çıkınca kullanıcı alanı düzeltip butona bastığında **hiçbir şey olmuyordu.**
- **Nedeni:**
  1. Fareye basılınca alan **odağını kaybediyor.**
  2. Kırmızı hata yazısı o anda **kayboluyor.**
  3. Buton **yukarı kayıyor**, fare bırakıldığında artık butonun üstünde değil → **tıklama boşa gidiyor.**
- **Çözüm:** Hata yazılarının yerini **baştan ayırdım** ve odak kaybında kontrolü kapattım.

> **Sonuç:** **65 tarayıcı testinin 65'i de** gerçek SQL Server 2022 üzerinde **geçti.**

---

## 10. Yayına Hazırladım

- **Önce** bir **`Dockerfile`** yazdım.
  - **Docker** = Uygulamayı, çalışması için gereken her şeyle birlikte **tek bir paket** hâline getirir.
- **Sonra** **`docker-compose.yml`** yazdım. Site ve SQL Server **tek komutla** birlikte çalışır:

```bash
cp .env.example .env
docker compose up -d --build
```

- **Ardından** **GitHub Actions** ile otomatik test kurdum.
  - **Bu şu demektir:** GitHub'a her kod yüklediğimde site **kendiliğinden** derlenir, gerçek SQL Server ile başlatılır ve sayfalar kontrol edilir. **İlk çalışma başarılı oldu.**

---

## 11. Güvenlik Önlemleri (Özet)

- **CSRF koruması** → Başka siteden sahte istek gönderilemez.
- **XSS koruması** → Biri gönderiye `<script>` yazsa bile **çalışmaz**, yazı olarak görünür.
- **Dosya kontrolü** → Uzantı, boyut ve dosya imzası kontrol edilir.
- **Sahiplik kontrolü** → Başkasının gönderisini **silemezsin.**
- **Hesap kilitleme** → Çok fazla yanlış şifre denenirse hesap **geçici olarak kilitlenir.**

---

## 12. Nasıl Çalıştırırım?

1. **Visual Studio 2022**'yi **"ASP.NET and web development"** iş yüküyle kur.
2. GitHub'dan `multhier/AI-Learner` deposunun **`kivilcim`** dalını indir.
3. `Kivilcim/Kivilcim.csproj` dosyasını aç.
4. **F5**'e bas. Veritabanı **otomatik** oluşur.

**Örnek hesaplar** (şifre hepsinde **`Demo1234`**):
- `ayse@ornek.com`
- `mehmet@ornek.com`
- `zeynep@ornek.com`

---

## 13. Ne Yapıldı, Ne Kaldı?

### ✅ Yapılanlar
- **Blog**, **gönderi** (metin, fotoğraf, **video**, alıntı, bağlantı), **akış**, **keşfet**, **etiket**, **arama**
- **Beğeni**, **reblog**, **yanıt**, **takip**, **gizli etiketler**
- **Anlık bildirimler** ve **mesajlaşma** (SignalR)
- **Türkçe** giriş, kayıt ve hesap sayfaları
- **Şifremi unuttum** (e-posta ile sıfırlama)
- **Gerçek SQL Server** testleri (65/65)
- **Docker** ve **GitHub Actions** ile yayına hazırlık

### ❌ Kalanlar
- **Canlı yayın:** Siteyi internete açmak için bir **barındırma hesabı** (Azure, VPS gibi) gerekiyor.
- **Gerçek e-posta gönderimi:** `appsettings.json` içine **SMTP bilgilerini** (ör. Gmail uygulama şifresi) girmen gerekiyor. Gerçek bir SMTP sunucusuyla henüz denenmedi.
