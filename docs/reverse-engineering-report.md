# Tersine Mühendislik Raporu: nvhny.com

Bu rapor, ASP.NET Core MVC + MSSQL ile yeniden yazımın neye dayandığını belgeler.
Analiz [REA (Reverse Engineer Anything)](https://github.com/morluto/rea) `rea-agents@6.10.1` ile yapıldı
(`npx rea-agents setup --client claude_code`). Sayfa içerikleri ayrıca HTTP ile indirilip metin olarak çıkarıldı.

## Yöntem

1. Siteyi açan headless Chromium (yalnızca 127.0.0.1:9222 üzerinde debug portu).
2. `rea list-browser-targets` → hedef sayfa bulundu.
3. `rea inspect-web-page --observation-ms 3000` → DOM, script ve kaynak envanteri.
4. `rea analyze-web-bundle` → JavaScript paketlerinin statik analizi (rota ve endpoint adayları).
5. Ana menüdeki 19 sayfa indirildi ve görünen metin, form alanları ve filtreler çıkarıldı.

| Kanıt | Evidence ID |
|---|---|
| Hedef listesi | `ev_f5e111e6c9b72f426817d1d6928471ad0b8a36392c1e0ba692f407dd6fcb28d9` |
| Sayfa incelemesi | `ev_8498cefabaf6b9677633ef7efe1ade4c20d6e83ac64197085934b023e0bd2ab6` |
| JS paket analizi | `ev_5601734677a096afb30bfc8c6238422ba8637c099aa87817ea5e1b408510cdae` |

## Gözlemler (REA tarafından doğrudan görülenler)

- **Platform:** Meta etiketi `author=RealtyMX`. Sayfalar sunucu tarafında üretiliyor (ColdFusion `index.cfm?page=...`).
- **Ön yüz:** Bootstrap 3, jQuery 1.x/jQuery UI, Owl Carousel, Nivo Slider, Fancybox, Lightbox, Chosen, multiple-select.
  Font Raleway. Tema rengi `#07988d` (`style/css/color/blue.css`).
- **JavaScript:** 15 script ayrıştırıldı, parse hatası yok. **SPA rotası veya veri API'si tespit edilmedi**:
  `route_inventory` ve `inferences` boş. Tüm gezinme klasik tam sayfa yüklemesiyle yapılıyor.
- **DOM:** Anasayfa 1.020 düğümden oluşuyor.

### Sınırlar

- Sayfa yakalaması `policy_filtered` / `attach_limited` durumunda. Gözlem penceresi sayfa yüklendikten sonra
  başladığı için ağ istekleri boş görünüyor. Bu, istek olmadığını kanıtlamaz.
- Bundle analizi `partial` (8.083 AST düğümü gezildi).
- Yönetim paneli, veritabanı şeması ve sunucu kodu dışarıdan görülemez. Aşağıdaki veri modeli,
  görünen alanlardan yapılmış bir **çıkarımdır**.

## Rota envanteri (eski → yeni)

| Orijinal | Yeni MVC rotası |
|---|---|
| `?page=properties&status=1,11,12&isMLS=1&isCompany=1` | `/properties/sales` |
| `?page=properties&status=2,21&isMLS=1&isCompany=1` | `/properties/rentals` |
| `?page=properties&...&isCommercial=1&type=commercial,retail,office,building,land,investment` | `/properties/commercial` |
| `?page=properties&...&openHouseChecked=1` | `/properties/open-house` |
| `?page=properties&status=2,21&...&ShortTerm=1` | `/properties/short-term` |
| `?page=agents` / `?page=agents&id=15` | `/agents` / `/agents/15` |
| `?page=content&cms=about` (cbb, disclaimer, relocation) | `/page/about` ... |
| `?page=resources&sub=buyers` (sellers, renters) | `/resources/buyers` ... |
| `?page=blog`, `?page=news`, `?page=testimonials` | `/blog`, `/news`, `/testimonials` |
| `?page=contact`, `?page=careers`, `?page=owners` | `/contact`, `/careers`, `/owners` |
| `?page=neighborhoods`, `?page=links`, `?page=florida`, `?page=northPort` | `/page/neighborhoods` ... |

`LegacyController`, eski `index.cfm` adreslerini 301 ile yeni rotalara yönlendirir.

## Çıkarılan veri modeli

| Görülen öğe | Tablo |
|---|---|
| İlan kartı: adres, daire, tip (Condo/Coop/Townhouse/House), semt, fiyat, BR/BT/FM, "Listing Courtesy Of", "App. Pending" | `Listings`, `ListingPhotos` |
| "More" filtresi: banyo, aile, kaynak (MLS/Şirket/Exclusive), durum, bina ve daire özellikleri, evcil hayvan, ft², aylık gider | `Listings` kolonları |
| Semt seçici (Bronx, Brooklyn, Manhattan, New Jersey, Queens + alt semtler) | `Areas`, `Neighborhoods` |
| Danışman listesi (Management / Agents), harf ve dil filtresi, profil, V-Card | `Agents` |
| Müşteri yorumları (tarih, müşteri, başlık, semt, danışmana bağlı) | `Testimonials` |
| Helpful Hints blogu (yazar, tarih, arşiv, sayfalama) ve Recent News | `Posts` (Category) |
| CMS sayfaları (about, cbb, disclaimer, relocation, resources) | `ContentPages` |
| Contact formu, danışman iletişim sekmesi, "List With Us" | `ContactMessages` |
| Interview Application (License Status: Taking Course / Licensed / Expired) | `CareerApplications` |
| "Create Alert" | `SearchAlerts` |
| Log In / Create your profile (Rentals/Sales ilgisi) | ASP.NET Identity + `ApplicationUser.InterestedIn` |
| Footer: şirket adı, adres, telefon, e-posta, sosyal medya | `SiteSettings` |

## Bilinçli farklar

- Gerçek şirkete ait metin, isim, fotoğraf ve logolar **kopyalanmadı**. Yerlerinde kurgusal demo içerik ve SVG
  görseller var. Marka bilgileri yönetim panelindeki **Ayarlar** sayfasından değiştirilir.
- Captcha yerine antiforgery token ve görünmez honeypot alanı kullanıldı.
- MLS veri akışı (RealtyMX entegrasyonu) yok. MLS ilanları panelden "MLS İlanı" kaynağıyla girilir.
- Arayüz metinleri Türkçe.
