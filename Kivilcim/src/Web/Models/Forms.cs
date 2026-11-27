using System.ComponentModel.DataAnnotations;
using Kivilcim.Bll.Rules;
using Kivilcim.Model;

namespace Kivilcim.Web.Models;

/// <summary>Gönderi oluşturma formu. Kurallar Bll'de; burada yalnızca form alanları ve üst sınırlar.</summary>
public class CreatePostViewModel
{
    public const int MaxImages = PostRules.MaxImagesPerPost;

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

    /// <summary>Cihazdan seçilen resimler (en fazla 30).</summary>
    [Display(Name = "Görseller")]
    public List<IFormFile> Images { get; set; } = new();

    /// <summary>İnternetten eklenen resim bağlantıları.</summary>
    public List<string> ImageUrls { get; set; } = new();

    [Display(Name = "Video")]
    public IFormFile? Video { get; set; }

    [StringLength(600)]
    [Display(Name = "Etiketler")]
    public string? Tags { get; set; }
}

/// <summary>Gönderi düzenleme formu: yalnızca metin alanları ve etiketler.</summary>
public class EditPostViewModel
{
    public int Id { get; set; }
    public PostType Type { get; set; }
    public List<string> Images { get; set; } = new();

    [StringLength(200, ErrorMessage = "Başlık en fazla 200 karakter olabilir.")]
    [Display(Name = "Başlık")]
    public string? Title { get; set; }

    [StringLength(10000, ErrorMessage = "Metin en fazla 10.000 karakter olabilir.")]
    [Display(Name = "Metin")]
    public string? Body { get; set; }

    [StringLength(2000)]
    [Display(Name = "Bağlantı")]
    public string? LinkUrl { get; set; }

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

/// <summary>Gönderi kartında içerik kısmına giden veri (kısmi görünüm parametresi).</summary>
public record PostContentModel(PostType Type, string? Title, string? Body, List<string> Images, string? VideoPath, string? LinkUrl);

/// <summary>Gönderi göndermek ya da yanıt yazmak için JSON istek gövdesi.</summary>
public record ChatSendRequest(string? Body);
