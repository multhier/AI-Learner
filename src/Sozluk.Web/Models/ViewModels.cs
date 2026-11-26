using System.ComponentModel.DataAnnotations;

namespace Sozluk.Web.Models;

public record EntryView(
    int Id, int TopicId, string TopicTitle, string TopicSlug,
    string AuthorName, UserRole AuthorRole, string Content,
    DateTime CreatedAt, DateTime? UpdatedAt,
    int UpVotes, int DownVotes, int FavoriteCount,
    short MyVote, bool IsMyFavorite, bool IsMine, int? Number = null);

public record GundemItem(int Id, string Title, string Slug, int Count);

public class TopicPageViewModel
{
    public Topic Topic { get; set; } = null!;
    public List<EntryView> Entries { get; set; } = new();
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public string Sort { get; set; } = "";
    public string? Author { get; set; }
    public NewEntryInput NewEntry { get; set; } = new();
}

public class EntryListViewModel
{
    public string Heading { get; set; } = "";
    public string? SubHeading { get; set; }
    public List<EntryView> Entries { get; set; } = new();
}

public class SearchViewModel
{
    public string Query { get; set; } = "";
    public List<GundemItem> Topics { get; set; } = new();
    public bool CanCreate { get; set; }
}

public class UserProfileViewModel
{
    public User User { get; set; } = null!;
    public string Tab { get; set; } = "entryler";
    public int EntryCount { get; set; }
    public int TopicCount { get; set; }
    public int TotalUp { get; set; }
    public List<EntryView> Entries { get; set; } = new();
    public int Page { get; set; }
    public int TotalPages { get; set; }
}

public class NewEntryInput
{
    public int TopicId { get; set; }

    [Required(ErrorMessage = "boş entry olmaz")]
    [StringLength(10000, MinimumLength = 2, ErrorMessage = "entry 2-10000 karakter olmalı")]
    public string Content { get; set; } = "";
}

public class NewTopicInput
{
    [Required(ErrorMessage = "başlık gerekli")]
    [StringLength(100, MinimumLength = 1)]
    public string Title { get; set; } = "";

    [Required(ErrorMessage = "ilk entry gerekli")]
    [StringLength(10000, MinimumLength = 2, ErrorMessage = "entry 2-10000 karakter olmalı")]
    public string Content { get; set; } = "";
}

public class LoginInput
{
    [Required(ErrorMessage = "kullanıcı adı gerekli")]
    public string UserName { get; set; } = "";

    [Required(ErrorMessage = "şifre gerekli"), DataType(DataType.Password)]
    public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }
}

public class RegisterInput
{
    [Required(ErrorMessage = "nick gerekli")]
    [StringLength(40, MinimumLength = 3, ErrorMessage = "nick 3-40 karakter olmalı")]
    [RegularExpression(@"^[a-zA-Z0-9çğıöşüÇĞİÖŞÜ ._-]+$", ErrorMessage = "nick'te geçersiz karakter var")]
    public string UserName { get; set; } = "";

    [Required(ErrorMessage = "e-posta gerekli"), EmailAddress(ErrorMessage = "geçersiz e-posta")]
    public string Email { get; set; } = "";

    [Required, StringLength(100, MinimumLength = 6, ErrorMessage = "şifre en az 6 karakter"), DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Compare(nameof(Password), ErrorMessage = "şifreler eşleşmiyor"), DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = "";
}

public class MessageThreadViewModel
{
    public User Other { get; set; } = null!;
    public List<Message> Messages { get; set; } = new();
}

public record ConversationItem(string OtherName, string LastBody, DateTime LastAt, int Unread);

public record CaylakItem(string UserName, DateTime CreatedAt, int EntryCount);
