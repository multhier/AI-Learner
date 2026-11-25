using System.ComponentModel.DataAnnotations;

namespace Kivilcim.Models;

public class PostCardViewModel
{
    public int Id { get; set; }
    public PostType Type { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
    public List<string> Images { get; set; } = new();
    public string? VideoPath { get; set; }
    public string? LinkUrl { get; set; }
    public DateTime CreatedAt { get; set; }

    public string AuthorId { get; set; } = "";
    public string AuthorBlogName { get; set; } = "";
    public string AuthorDisplayName { get; set; } = "";
    public string? AuthorAvatar { get; set; }

    public List<string> Tags { get; set; } = new();

    public int LikeCount { get; set; }
    public int ReplyCount { get; set; }
    public int ReblogCount { get; set; }
    public bool LikedByMe { get; set; }

    /// <summary>Bu gönderi bir reblog ise asıl gönderi. Body bu durumda reblog yorumudur.</summary>
    public OriginalPostViewModel? Original { get; set; }
    public bool IsReblog => Original != null;

    public int NoteCount => LikeCount + ReplyCount + ReblogCount;
}

public class OriginalPostViewModel
{
    public int Id { get; set; }
    public PostType Type { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
    public List<string> Images { get; set; } = new();
    public string? VideoPath { get; set; }
    public string? LinkUrl { get; set; }
    public string AuthorBlogName { get; set; } = "";
    public string AuthorDisplayName { get; set; } = "";
    public string? AuthorAvatar { get; set; }
}

/// <summary>_PostContent kısmi görünümüne giden gönderi içeriği.</summary>
public record PostContentModel(PostType Type, string? Title, string? Body, List<string> Images, string? VideoPath, string? LinkUrl);

public class PostListViewModel
{
    public List<PostCardViewModel> Posts { get; set; } = new();
    public string? NextPageUrl { get; set; }
    public string EmptyMessage { get; set; } = "Henüz gönderi yok.";
}

public class HomeViewModel
{
    public bool IsDashboard { get; set; }
    public PostListViewModel Feed { get; set; } = new();
    public List<TagCount> TrendingTags { get; set; } = new();
    public List<BlogSuggestion> Suggestions { get; set; } = new();
}

public record TagCount(string Name, int Count);

public record BlogSuggestion(string BlogName, string DisplayName, string? AvatarPath, string? Bio);

public class ExploreViewModel
{
    public string Heading { get; set; } = "Keşfet";
    public string? Query { get; set; }
    public string? Tag { get; set; }
    public PostListViewModel Results { get; set; } = new();
    public List<TagCount> TrendingTags { get; set; } = new();
}

public class BlogViewModel
{
    public string BlogName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? Bio { get; set; }
    public string? AvatarPath { get; set; }
    public DateTime JoinedAt { get; set; }
    public int PostCount { get; set; }
    public int FollowerCount { get; set; }
    public int FollowingCount { get; set; }
    public bool IsMe { get; set; }
    public bool IsFollowing { get; set; }
    public PostListViewModel Posts { get; set; } = new();
}

public class PostDetailsViewModel
{
    public PostCardViewModel Post { get; set; } = null!;
    public List<ReplyViewModel> Replies { get; set; } = new();
    public List<NoteViewModel> RecentNotes { get; set; } = new();
}

public record ReplyViewModel(string BlogName, string DisplayName, string? AvatarPath, string Body, DateTime CreatedAt);

public record NoteViewModel(string Kind, string BlogName, DateTime CreatedAt);

public class CreatePostViewModel
{
    public PostType Type { get; set; } = PostType.Text;

    [StringLength(200, ErrorMessage = "Başlık en fazla 200 karakter olabilir.")]
    [Display(Name = "Başlık")]
    public string? Title { get; set; }

    [StringLength(10000, ErrorMessage = "Metin en fazla 10.000 karakter olabilir.")]
    [Display(Name = "Metin")]
    public string? Body { get; set; }

    [StringLength(2000)]
    [Display(Name = "Bağlantı")]
    public string? LinkUrl { get; set; }

    public const int MaxImages = 30;

    /// <summary>Cihazdan yüklenen görseller.</summary>
    [Display(Name = "Görseller")]
    public List<IFormFile> Images { get; set; } = new();

    /// <summary>İnternetten eklenen görsel bağlantıları.</summary>
    public List<string> ImageUrls { get; set; } = new();

    [Display(Name = "Video")]
    public IFormFile? Video { get; set; }

    [StringLength(600)]
    [Display(Name = "Etiketler")]
    public string? Tags { get; set; }
}

public class ProfileSettingsViewModel
{
    [Required(ErrorMessage = "Blog adı zorunlu.")]
    [Display(Name = "Blog adı")]
    public string BlogName { get; set; } = "";

    [Required(ErrorMessage = "Görünen ad zorunlu.")]
    [StringLength(64)]
    [Display(Name = "Görünen ad")]
    public string DisplayName { get; set; } = "";

    [StringLength(500)]
    [Display(Name = "Hakkımda")]
    public string? Bio { get; set; }

    [Display(Name = "Profil fotoğrafı")]
    public IFormFile? Avatar { get; set; }

    public string? CurrentAvatarPath { get; set; }
    public List<string> HiddenTags { get; set; } = new();
}

public record NotificationViewModel(
    int Id, NotificationType Type, string ActorBlogName, string ActorDisplayName, string? ActorAvatar,
    int? PostId, string? PostPreview, bool IsRead, DateTime CreatedAt)
{
    public string Text => Kivilcim.Services.NotificationService.Describe(Type);
    public string Url => PostId.HasValue ? $"/post/{PostId}" : $"/blog/{ActorBlogName}";
}

public record ConversationViewModel(
    string BlogName, string DisplayName, string? AvatarPath, string LastMessage, bool LastFromMe,
    DateTime LastAt, int UnreadCount);

public record MessageViewModel(int Id, bool FromMe, string Body, DateTime CreatedAt);

public class ThreadViewModel
{
    public string BlogName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? AvatarPath { get; set; }
    public List<MessageViewModel> Messages { get; set; } = new();
}
