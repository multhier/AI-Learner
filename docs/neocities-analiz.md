# Neocities davranış analizi (REA ile)

Bu belge, WebKöy'ü tasarlarken örnek aldığımız Neocities'in **dışarıdan gözlenebilen davranışını** özetler.
Neocities'in kaynak kodu kopyalanmadı; yalnızca herkese açık sayfalar bir tarayıcıda gezildi.

## Yöntem

- Araç: [REA](https://github.com/morluto/rea) `rea-agents@6.10.1`, `capture-browser-scenario` (CDP connect modu, Chromium 158).
- Senaryo: [`rea-neocities-scenario.json`](rea-neocities-scenario.json). 12 adımda herkese açık sayfalar gezildi; her adımda URL, DOM ve erişilebilirlik ağacı, ayrıca ağ olayları kaydedildi.
- Kanıt: `ev_882383898b75345544d0cf316ef0e0fcd7f4bd99f990000486851260e6ace970` (controlled replay, 2026-11-25). 567 istek, 565 yanıt.
- Kapsam dışı: giriş gerektiren sayfalar (panel, dosya yöneticisi, editör, ayarlar). Gerçek bir hesap açılmadı, hiçbir form gönderilmedi. Kullanıcı sitelerinin (`*.neocities.org`) yanıt başlıkları da ağ izni olmadığı için incelenmedi.

Etiketler: **[G]** gözlem (kanıtta doğrudan var) · **[Ç]** çıkarım · **[?]** bilinmiyor.

## Sayfa ve rota haritası

| Adım | URL | Durum | Not |
|---|---|---|---|
| home | `/` | 200 | Tanıtım ve kayıt formu aynı sayfada [G] |
| browse | `/browse` | 200 | Site kartları, sıralama ve etiket filtresi [G] |
| browse_tag | `/browse?tag=art&sort_by=followers` | 404 | `followers` geçerli bir sıralama değil; geçersiz değer 404 döndürüyor [Ç] |
| signin | `/signin` | 200 | `POST /signin`: `username`, `password`, `remember_computer` [G] |
| profile | `/site/neocities` | 200 | Site profili [G] |
| supporter | `/supporter` | 200 | Ücretsiz ve ücretli plan karşılaştırması [G] |
| api | `/api` | 200 | Geliştirici API dokümantasyonu [G] |
| tutorials, about, terms | | 200 | Statik içerik [G] |
| permanent | `/permanent-web` | 404 | [G] |
| forgot | `/password_reset` | 200 | `POST /send_password_reset`: `email` [G] |

Üst menü: Websites (`/browse`), Search (`/search`), Activity (`/activity`), Learn, Support Us; ayrıca karanlık mod düğmesi [G].

## Kayıt

- Form ana sayfada. Alanlar: `username` (`.neocities.org` son ekiyle), `new_tags_string` (ilgi alanları ve site konuları), `password`, `email` [G].
- Gizli `prevent_autofill_username` ve `prevent_autofill_password` alanları var. Tarayıcının otomatik doldurmasını engellemek için kullanılıyor olmalı [Ç].
- Sayfa `https://hcaptcha.com/1/api.js` yüklüyor. Kayıt sırasında **hCaptcha** kullanılıyor [G]. Bu ortamda betiğin kendisi yüklenemedi (ağ izni yok), bu yüzden doğrulama akışı gözlenmedi [?].
- Sayfa "1,853,100 web sites" sayısını gösteriyor [G].

## Keşfet (`/browse`)

- `sort_by` değerleri: `special_sauce` (varsayılan), `random`, `most_followed`, `last_updated`, `views`, `tipping_enabled`, `oldest`, `robots` [G].
- Etiket filtresi: `tag` metin kutusu [G].
- "Special sauce" sıralamasının formülü bilinmiyor [?]. Muhtemelen takipçi sayısı, güncellik ve görüntülenmenin bir karışımı [Ç].

## Site profili (`/site/{ad}`)

- Başlık, site adresi, ekran görüntüsü önizlemesi [G].
- Sayaçlar: **views**, **followers**, **updates**, **tips** [G].
- "New Followers" listesi [G].
- **Etkinlik akışı ve duvar:** "X, Y'yi takip etti", "4 updates" (site güncelleme olayları) ve profil duvarına yazılmış mesajlar. Her öğe beğenilebiliyor ("6 likes") [G]. Sayfalama 10 sayfaya kadar gidiyor [G].
- "Website Stats": son güncelleme, açılış tarihi; takipçi avatarları, `/site/{ad}/followers` [G].

## Planlar (`/supporter`)

| Özellik | Ücretsiz | Supporter ($5/ay) |
|---|---|---|
| Depolama | 1 GB | 50 GB |
| Bant genişliği | 200 GB | 3000 GB |
| Site düzenleyici, CLI, etiketler, RSS, takip, profil, tek tıkla yedek, hotlink koruması, yapay zekâ botu engelleme | var | var |
| İstatistik geçmişi, WebDAV, SFTP, özel alan adı ve SSL, birden fazla site, dosya türü kısıtı olmaması, CORS, daha gevşek CSP, e-posta desteği | yok | var |

Ücretsiz sitelerde dosya türü kısıtı, CORS kapalı ve sıkı bir Content-Security-Policy olduğu anlaşılıyor [Ç].

## Geliştirici API'si (`/api`)

Kimlik doğrulama: `Authorization: Bearer API_KEY`. Anahtar site ayarlarından alınıyor [G].
GET istekleri query string kullanıyor. POST istekleri form-urlencoded, `/api/upload` ise multipart [G].
Başarılı yanıtlar `200 {"result":"success", ...}` döndürüyor. Hatalar şöyle [G]:
- `400`: `{"result":"error","error_type":"...","message":"..."}`
- `403`: geçersiz kimlik bilgisi
- `404`: bilinmeyen uç nokta

| Uç nokta | Davranış [G] |
|---|---|
| `POST /api/upload` | Form alanının adı hedef yol. Eksik klasörler oluşturulur. Önce tüm dosyalar doğrulanır, sonra yazılır. Dosya ve istek başına 100 MB. Aynı içerik yeniden yüklenirse "zaten güncel" yanıtı döner. |
| `POST /api/create_directory` | `path`. Üst klasörler oluşturulur. Hedef varsa hata verir. |
| `POST /api/rename` | `path`, `new_path`. Hedefin üst klasörü önceden var olmalı, üzerine yazılamaz, kök `index.html` taşınamaz. Klasör kendi içine taşınamaz ve `.html` ya da `.htm` ile biten bir ad alamaz. İzin verilmeyen bir uzantıya da çevrilemez. |
| `POST /api/delete` | Birden fazla `filenames[]`. Önce tüm yollar kontrol edilir; biri geçersizse hiçbir şey silinmez. Kök ve kök `index.html` korunur. |
| `GET /api/list` | `path` verilmezse tüm site özyinelemeli listelenir, verilirse yalnızca o klasörün çocukları. Alanlar: `path`, `is_directory`, `created_at`, `updated_at` (RFC 2822); dosyalarda ayrıca `size` ve `sha1_hash`. |
| `GET /api/download` | Ham dosya. `ETag` SHA-1 değeri, `Cache-Control: private, no-store`. |
| `GET /api/info` | `sitename` verilirse kimlik doğrulama gerekmez. Alanlar: `sitename`, `views`, `hits`, `created_at`, `last_updated`, `domain`, `supporter`, `tags`. |
| `POST /api/upload_hash` | Alan adı yol, değer SHA-1. Her yol için içerik aynıysa `true`, değilse `false`. |

Kullanım kuralları: güncellemeleri en sık on dakikada bir yapın, paralel istek atmayın, sıralamayı manipüle etmeyin, siteleri kazımayın [G].
**views** ile **hits** ayrı tutuluyor [G]. views tekil ziyaretçileri, hits tüm istekleri sayıyor olmalı [Ç].

## WebKöy ile karşılaştırma

| Neocities davranışı | WebKöy (önce) | WebKöy (bu güncellemeden sonra) |
|---|---|---|
| Kayıtta CAPTCHA | yok | hCaptcha (anahtar verilirse etkin) |
| Kayıtta etiket alanı | yok | var |
| Şifre sıfırlama | yok | var |
| API anahtarı ve 8 API uç noktası | yok | var, Neocities ile aynı sözleşme |
| Dosyalarda SHA-1 ve aynı içeriği atlama | yok | var |
| views ile hits ayrımı | yalnızca views | ikisi de |
| Keşfet sıralamaları | 5 | `random` ve `oldest` eklendi, adlar Neocities ile uyumlu |
| Yeniden adlandırma kuralları (üst klasör, .html klasör adı) | kısmen | Neocities ile aynı |
| Çoklu silmede ya hep ya hiç | tek dosya | API'de var |
| Etkinlik akışı, güncelleme olayları, duvar beğenileri | yok | **yapılmadı**, yol haritasında |
| Bahşiş, supporter planı, özel alan adı, WebDAV, SFTP | yok | **yapılmadı**, yol haritasında |
| Site ekran görüntüleri | yok | **yapılmadı**, yol haritasında |
