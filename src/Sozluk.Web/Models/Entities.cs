using System.ComponentModel.DataAnnotations;

namespace Sozluk.Web.Models;

public enum UserRole
{
    Caylak = 0,
    Yazar = 1,
    Moderator = 2,
    Admin = 3
}

public class User
{
    public int Id { get; set; }

    [MaxLength(40)]
    public string UserName { get; set; } = "";

    [MaxLength(256)]
    public string Email { get; set; } = "";

    public string PasswordHash { get; set; } = "";

    public UserRole Role { get; set; } = UserRole.Caylak;

    [MaxLength(1000)]
    public string? Bio { get; set; }

    public bool IsBanned { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Entry> Entries { get; set; } = new List<Entry>();
}

public class Topic
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string Title { get; set; } = "";

    [MaxLength(120)]
    public string Slug { get; set; } = "";

    public int CreatedById { get; set; }
    public User? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gündem sıralaması için son entry zamanı.</summary>
    public DateTime LastEntryAt { get; set; } = DateTime.UtcNow;

    /// <summary>Silinmemiş entry sayısı (denormalize).</summary>
    public int EntryCount { get; set; }

    public bool IsLocked { get; set; }

    public ICollection<Entry> Entries { get; set; } = new List<Entry>();
}

public class Entry
{
    public int Id { get; set; }

    public int TopicId { get; set; }
    public Topic? Topic { get; set; }

    public int AuthorId { get; set; }
    public User? Author { get; set; }

    [MaxLength(10000)]
    public string Content { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public int UpVotes { get; set; }
    public int DownVotes { get; set; }
    public int FavoriteCount { get; set; }

    public ICollection<Vote> Votes { get; set; } = new List<Vote>();
}

public class Vote
{
    public int EntryId { get; set; }
    public Entry? Entry { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    /// <summary>+1 veya -1</summary>
    public short Value { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Favorite
{
    public int EntryId { get; set; }
    public Entry? Entry { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Message
{
    public int Id { get; set; }

    public int SenderId { get; set; }
    public User? Sender { get; set; }

    public int ReceiverId { get; set; }
    public User? Receiver { get; set; }

    [MaxLength(5000)]
    public string Body { get; set; } = "";

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Report
{
    public int Id { get; set; }

    public int EntryId { get; set; }
    public Entry? Entry { get; set; }

    public int ReporterId { get; set; }
    public User? Reporter { get; set; }

    [MaxLength(500)]
    public string Reason { get; set; } = "";

    public bool IsResolved { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
