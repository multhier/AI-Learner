using Microsoft.AspNetCore.Identity;

namespace Kivilcim.Model;

public class ApplicationUser : IdentityUser
{
    /// <summary>Blog adresinde görünen benzersiz ad: /blog/{BlogName}</summary>
    public string? BlogName { get; set; }
    public string DisplayName { get; set; } = "";
    public string? Bio { get; set; }
    public string? AvatarPath { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Post> Posts { get; set; } = new();
    public List<Follow> Followers { get; set; } = new();
    public List<Follow> Following { get; set; } = new();
    public List<HiddenTag> HiddenTags { get; set; } = new();
}

public enum PostType
{
    Text = 0,
    Photo = 1,
    Quote = 2,
    Link = 3,
    Video = 4
}

public class Post
{
    public int Id { get; set; }

    public string AuthorId { get; set; } = "";
    public ApplicationUser Author { get; set; } = null!;

    public PostType Type { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
    public string? VideoPath { get; set; }
    public string? LinkUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Yeniden paylaşımsa (reblog) asıl gönderinin Id'si. Her zaman zincirin köküne işaret eder.</summary>
    public int? ReblogOfId { get; set; }
    public Post? ReblogOf { get; set; }
    public List<Post> Reblogs { get; set; } = new();

    /// <summary>Fotoğraf gönderisinin görselleri (en fazla 30), sırasıyla.</summary>
    public List<PostImage> Images { get; set; } = new();
    public List<PostTag> PostTags { get; set; } = new();
    public List<Like> Likes { get; set; } = new();
    public List<Reply> Replies { get; set; } = new();
}

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public List<PostTag> PostTags { get; set; } = new();
}

public class PostTag
{
    public int PostId { get; set; }
    public Post Post { get; set; } = null!;
    public int TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}

public class Like
{
    public string UserId { get; set; } = "";
    public ApplicationUser User { get; set; } = null!;
    public int PostId { get; set; }
    public Post Post { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Follow
{
    public string FollowerId { get; set; } = "";
    public ApplicationUser Follower { get; set; } = null!;
    public string FolloweeId { get; set; } = "";
    public ApplicationUser Followee { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Reply
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public Post Post { get; set; } = null!;
    public string UserId { get; set; } = "";
    public ApplicationUser User { get; set; } = null!;
    public string Body { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Kullanıcının akışında görmek istemediği etiketler.</summary>
public class HiddenTag
{
    public string UserId { get; set; } = "";
    public ApplicationUser User { get; set; } = null!;
    public string TagName { get; set; } = "";
}

public enum NotificationType
{
    Like = 0,
    Reblog = 1,
    Reply = 2,
    Follow = 3
}

/// <summary>Bir kullanıcıya giden bildirim: "X gönderini beğendi" gibi.</summary>
public class Notification
{
    public int Id { get; set; }
    public string RecipientId { get; set; } = "";
    public ApplicationUser Recipient { get; set; } = null!;
    public string ActorId { get; set; } = "";
    public ApplicationUser Actor { get; set; } = null!;
    public NotificationType Type { get; set; }
    public int? PostId { get; set; }
    public Post? Post { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>İki kullanıcı arasındaki özel mesaj.</summary>
public class Message
{
    public int Id { get; set; }
    public string SenderId { get; set; } = "";
    public ApplicationUser Sender { get; set; } = null!;
    public string RecipientId { get; set; } = "";
    public ApplicationUser Recipient { get; set; } = null!;
    public string Body { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
}

/// <summary>Fotoğraf gönderisindeki tek bir görsel: yüklenen dosya ya da internet bağlantısı.</summary>
public class PostImage
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public Post Post { get; set; } = null!;
    /// <summary>Yüklenen dosyada "/uploads/posts/..." yolu, internetten eklenende tam adres.</summary>
    public string Url { get; set; } = "";
    public bool IsExternal { get; set; }
    public int SortOrder { get; set; }
}
