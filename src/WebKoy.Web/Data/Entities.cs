using Microsoft.AspNetCore.Identity;

namespace WebKoy.Web.Data;

public class ApplicationUser : IdentityUser
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Site? Site { get; set; }
}

/// <summary>Bir kullanıcının yayınladığı statik site. Name aynı zamanda alt alan adıdır.</summary>
public class Site
{
    public int Id { get; set; }
    public string OwnerId { get; set; } = default!;
    public ApplicationUser Owner { get; set; } = default!;

    public string Name { get; set; } = default!;
    public string? Title { get; set; }
    public string? Description { get; set; }

    /// <summary>Tekil ziyaretçi sayısı (IP başına günde bir).</summary>
    public long Views { get; set; }
    /// <summary>Sunulan tüm dosya istekleri.</summary>
    public long Hits { get; set; }
    public long StorageBytes { get; set; }

    /// <summary>API anahtarının SHA-256 özeti; anahtarın kendisi saklanmaz.</summary>
    public string? ApiKeyHash { get; set; }
    public DateTime? ApiKeyCreatedAt { get; set; }

    public bool IsBanned { get; set; }
    public string? BanReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<SiteFile> Files { get; set; } = new();
    public List<SiteTag> Tags { get; set; } = new();
    public List<Follow> Followers { get; set; } = new();
    public List<Like> Likes { get; set; } = new();
    public List<Comment> Comments { get; set; } = new();
}

/// <summary>Dosya meta verisi. İçerik IFileStorage'da durur, veritabanında değil.</summary>
public class SiteFile
{
    public int Id { get; set; }
    public int SiteId { get; set; }
    public Site Site { get; set; } = default!;

    /// <summary>Normalleştirilmiş göreli yol, ör. "img/logo.png".</summary>
    public string Path { get; set; } = default!;
    public bool IsDirectory { get; set; }
    public long Size { get; set; }
    /// <summary>İçeriğin SHA-1 özeti (küçük harf hex); klasörlerde null.</summary>
    public string? Sha1 { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public List<SiteTag> Sites { get; set; } = new();
}

public class SiteTag
{
    public int SiteId { get; set; }
    public Site Site { get; set; } = default!;
    public int TagId { get; set; }
    public Tag Tag { get; set; } = default!;
}

public class Follow
{
    public string UserId { get; set; } = default!;
    public ApplicationUser User { get; set; } = default!;
    public int SiteId { get; set; }
    public Site Site { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Like
{
    public string UserId { get; set; } = default!;
    public ApplicationUser User { get; set; } = default!;
    public int SiteId { get; set; }
    public Site Site { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Comment
{
    public int Id { get; set; }
    public int SiteId { get; set; }
    public Site Site { get; set; } = default!;
    public string AuthorId { get; set; } = default!;
    public ApplicationUser Author { get; set; } = default!;
    public string Body { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum ReportReason { Phishing, Malware, Spam, Illegal, Harassment, Other }

public class Report
{
    public int Id { get; set; }
    public int SiteId { get; set; }
    public Site Site { get; set; } = default!;
    public string? ReporterId { get; set; }
    public ApplicationUser? Reporter { get; set; }
    public ReportReason Reason { get; set; }
    public string? Details { get; set; }
    public bool Resolved { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
