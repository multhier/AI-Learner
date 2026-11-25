using System.ComponentModel.DataAnnotations;
using WebKoy.Web.Data;

namespace WebKoy.Web.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Site adı gerekli.")]
    [Display(Name = "Site adı")]
    public string SiteName { get; set; } = "";

    [Required(ErrorMessage = "E-posta gerekli."), EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Şifre gerekli."), DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Şifre en az 8 karakter olmalı.")]
    [Display(Name = "Şifre")]
    public string Password { get; set; } = "";

    [DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "Şifreler eşleşmiyor.")]
    [Display(Name = "Şifre (tekrar)")]
    public string ConfirmPassword { get; set; } = "";

    [Range(typeof(bool), "true", "true", ErrorMessage = "Kullanım koşullarını kabul etmelisiniz.")]
    public bool AcceptTerms { get; set; }
}

public class LoginViewModel
{
    [Required(ErrorMessage = "Site adı veya e-posta gerekli.")]
    [Display(Name = "Site adı veya e-posta")]
    public string Login { get; set; } = "";

    [Required(ErrorMessage = "Şifre gerekli."), DataType(DataType.Password)]
    [Display(Name = "Şifre")]
    public string Password { get; set; } = "";

    [Display(Name = "Beni hatırla")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public class SiteCardViewModel
{
    public string Name { get; set; } = "";
    public string? Title { get; set; }
    public string? Description { get; set; }
    public long Views { get; set; }
    public int Followers { get; set; }
    public int Likes { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Url { get; set; } = "";
    public List<string> Tags { get; set; } = new();
}

public class BrowseViewModel
{
    public List<SiteCardViewModel> Sites { get; set; } = new();
    public string Sort { get; set; } = "updated";
    public string? Tag { get; set; }
    public string? Query { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; }
    public List<string> PopularTags { get; set; } = new();
}

public class HomeViewModel
{
    public List<SiteCardViewModel> Recent { get; set; } = new();
    public int SiteCount { get; set; }
}

public class DashboardViewModel
{
    public Site Site { get; set; } = default!;
    public string Dir { get; set; } = "";
    public List<SiteFile> Items { get; set; } = new();
    public long QuotaBytes { get; set; }
    public long MaxFileBytes { get; set; }
    public string SiteUrl { get; set; } = "";
    public IEnumerable<(string Name, string Path)> Breadcrumbs
    {
        get
        {
            if (Dir.Length == 0) yield break;
            var parts = Dir.Split('/');
            for (var i = 0; i < parts.Length; i++)
                yield return (parts[i], string.Join('/', parts[..(i + 1)]));
        }
    }
    public int UsagePercent => QuotaBytes == 0 ? 0 : (int)Math.Min(100, Site.StorageBytes * 100 / QuotaBytes);
}

public class EditFileViewModel
{
    public string Path { get; set; } = "";
    public string Content { get; set; } = "";
    public string SiteUrl { get; set; } = "";
    public string Language { get; set; } = "";
}

public class SiteSettingsViewModel
{
    [StringLength(100, ErrorMessage = "Başlık en fazla 100 karakter olabilir.")]
    [Display(Name = "Site başlığı")]
    public string? Title { get; set; }

    [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [Display(Name = "Etiketler (virgülle ayırın, en fazla 5)")]
    public string? Tags { get; set; }

    public string Name { get; set; } = "";
}

public class SiteProfileViewModel
{
    public Site Site { get; set; } = default!;
    public string Url { get; set; } = "";
    public int Followers { get; set; }
    public int Likes { get; set; }
    public bool IsFollowing { get; set; }
    public bool IsLiked { get; set; }
    public bool IsOwner { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<CommentViewModel> Comments { get; set; } = new();
}

public class CommentViewModel
{
    public int Id { get; set; }
    public string Body { get; set; } = "";
    public string AuthorSite { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public bool CanDelete { get; set; }
}

public class ReportInputModel
{
    public ReportReason Reason { get; set; }
    [StringLength(2000)]
    public string? Details { get; set; }
}
