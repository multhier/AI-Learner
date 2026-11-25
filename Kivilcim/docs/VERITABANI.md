# Kıvılcım · Veritabanı Şeması

Kıvılcım, **Microsoft SQL Server** üzerinde **11 uygulama tablosu** kullanır. Bunlara ASP.NET Core Identity'nin kendi tabloları (roller, oturum açma bilgileri vb.) eklenir.

![Kıvılcım veritabanı şeması](veritabani.png)

> Görselin etkileşimli hâli: [veritabani.html](veritabani.html) (indirip tarayıcıda açabilirsin).

## Şemayı Nasıl Okurum?

- **Her kutu bir tablo**, kutunun içindeki her satır **bir sütun**dur.
- **PK** (Primary Key, birincil anahtar) = Satırı benzersiz tanımlayan alan. Örnek: gönderinin numarası.
- **FK** (Foreign Key, yabancı anahtar) = Başka bir tablodaki satırı gösteren alan. Örnek: `Posts.AuthorId` → gönderiyi yazan kullanıcı.
- **UK** (Unique Key) = Aynı değer iki kez olamaz. Örnek: iki kullanıcı aynı **blog adını** alamaz.
- **1 ──── N** = Bir kayda **birden çok** kayıt bağlanır. Örnek: **bir** kullanıcının **çok** gönderisi olur.

## Tablolar

| Tablo | Ne saklar? | Anahtar |
|---|---|---|
| **AspNetUsers** | Kullanıcılar: e-posta, şifre özeti, blog adı, profil | `Id` |
| **Posts** | Gönderiler: tür, başlık, metin, video, bağlantı | `Id` |
| **PostImages** | Fotoğraf gönderisinin resimleri (en fazla 30); yüklenen dosya ya da internet bağlantısı | `Id` |
| **Tags** | Etiket adları (`kedi`, `yaz tatili`) | `Id` |
| **PostTags** | Hangi gönderide hangi etiket var | `PostId` + `TagId` |
| **Likes** | Kim hangi gönderiyi beğendi | `UserId` + `PostId` |
| **Replies** | Gönderilere yazılan yanıtlar | `Id` |
| **Follows** | Kim kimi takip ediyor | `FollowerId` + `FolloweeId` |
| **HiddenTags** | Kullanıcının akışında görmek istemediği etiketler | `UserId` + `TagName` |
| **Notifications** | Bildirimler: "X gönderini beğendi" | `Id` |
| **Messages** | Kullanıcılar arası özel mesajlar | `Id` |

## Önemli Noktalar

- **İki sütunlu anahtarlar:** `Likes` tablosunda anahtar `UserId + PostId`'dir.
  - **Bu şu demektir:** Bir kullanıcı aynı gönderiyi **iki kez beğenemez**; veritabanı buna izin vermez.
- **Reblog:** `Posts.ReblogOfId` yine `Posts` tablosunu gösterir (tablo kendine bağlanır).
  - Reblog'un reblog'u yapılsa bile **her zaman asıl gönderiye** bağlanır.
- **Çoklu resim:** Bir fotoğraf gönderisinin **birden çok** resmi olabilir; bu yüzden resimler ayrı **`PostImages`** tablosunda tutulur.
  - `IsExternal = 1` ise resim **internetten eklenmiş bir bağlantıdır**, sunucuya indirilmez.
  - `SortOrder` resimlerin gösterilme sırasıdır.
- **Çoka çok ilişki:** Bir gönderinin çok etiketi, bir etiketin çok gönderisi olabilir. Bu yüzden araya **`PostTags`** tablosu konuldu.
- **Silme kuralları:** SQL Server bir tabloya giden **birden fazla otomatik silme (cascade) yolunu** kabul etmez.
  - Bu yüzden kullanıcı silinirken beğenileri, yanıtları, takipleri, mesajları ve bildirimleri **uygulama kodu** temizler (`ApplicationDbContext.SaveChangesAsync`).
- **Tür alanları (`Type`) sayı olarak saklanır:**
  - `Posts.Type`: 0 Metin · 1 Fotoğraf · 2 Alıntı · 3 Bağlantı · 4 Video
  - `Notifications.Type`: 0 Beğeni · 1 Reblog · 2 Yanıt · 3 Takip

## Migration'lar

| Migration | Ne ekledi? |
|---|---|
| `InitialCreate` | Kullanıcı, gönderi, etiket, beğeni, yanıt, takip, gizli etiket tabloları |
| `NotificationsMessagesVideo` | `Notifications` ve `Messages` tabloları, `Posts.VideoPath` sütunu |
| `PostImagesGallery` | `PostImages` tablosu; eski gönderilerin tek resmi bu tabloya taşındı, `Posts.ImagePath` kaldırıldı |

## Metin Olarak Şema (Mermaid)

GitHub bu bölümü otomatik olarak diyagrama çevirir.

```mermaid
erDiagram
    AspNetUsers ||--o{ Posts : "yazar"
    Posts |o--o{ Posts : "reblog"
    Posts ||--o{ PostImages : "resimleri"
    Posts ||--o{ PostTags : "etiketlenir"
    Tags ||--o{ PostTags : "kullanilir"
    AspNetUsers ||--o{ Likes : "begenir"
    Posts ||--o{ Likes : "begenilir"
    Posts ||--o{ Replies : "yanitlanir"
    AspNetUsers ||--o{ Replies : "yazar"
    AspNetUsers ||--o{ Follows : "takip eder"
    AspNetUsers ||--o{ Follows : "takip edilir"
    AspNetUsers ||--o{ HiddenTags : "gizler"
    AspNetUsers ||--o{ Notifications : "alir"
    AspNetUsers ||--o{ Notifications : "tetikler"
    Posts |o--o{ Notifications : "hakkinda"
    AspNetUsers ||--o{ Messages : "gonderir"
    AspNetUsers ||--o{ Messages : "alir"

    AspNetUsers {
        nvarchar(450) Id PK
        nvarchar(256) UserName
        nvarchar(256) Email
        nvarchar(max) PasswordHash
        nvarchar(32) BlogName UK "benzersiz blog adi"
        nvarchar(64) DisplayName
        nvarchar(500) Bio
        nvarchar(300) AvatarPath
        datetime2 CreatedAt
    }
    Posts {
        int Id PK
        nvarchar(450) AuthorId FK
        int Type "0 Metin 1 Foto 2 Alinti 3 Link 4 Video"
        nvarchar(200) Title
        nvarchar(max) Body
        nvarchar(300) VideoPath
        nvarchar(2000) LinkUrl
        int ReblogOfId FK "asil gonderi"
        datetime2 CreatedAt
    }
    PostImages {
        int Id PK
        int PostId FK
        nvarchar(2000) Url "dosya yolu ya da internet adresi"
        bit IsExternal
        int SortOrder
    }
    Tags {
        int Id PK
        nvarchar(50) Name UK
    }
    PostTags {
        int PostId PK,FK
        int TagId PK,FK
    }
    Likes {
        nvarchar(450) UserId PK,FK
        int PostId PK,FK
        datetime2 CreatedAt
    }
    Follows {
        nvarchar(450) FollowerId PK,FK "takip eden"
        nvarchar(450) FolloweeId PK,FK "takip edilen"
        datetime2 CreatedAt
    }
    Replies {
        int Id PK
        int PostId FK
        nvarchar(450) UserId FK
        nvarchar(2000) Body
        datetime2 CreatedAt
    }
    HiddenTags {
        nvarchar(450) UserId PK,FK
        nvarchar(50) TagName PK
    }
    Notifications {
        int Id PK
        nvarchar(450) RecipientId FK "bildirimi alan"
        nvarchar(450) ActorId FK "eylemi yapan"
        int Type "0 Begeni 1 Reblog 2 Yanit 3 Takip"
        int PostId FK
        bit IsRead
        datetime2 CreatedAt
    }
    Messages {
        int Id PK
        nvarchar(450) SenderId FK
        nvarchar(450) RecipientId FK
        nvarchar(4000) Body
        datetime2 CreatedAt
        datetime2 ReadAt "okunma zamani"
    }
```
