using Kivilcim.Bll.Media;
using Kivilcim.Model;

namespace Kivilcim.Bll.Dtos;

/// <summary>Yeni gönderi isteği. Web katmanı formu buna çevirir.</summary>
public sealed record CreatePostCommand(
    PostType Type,
    string? Title,
    string? Body,
    string? LinkUrl,
    string? Tags,
    IReadOnlyList<MediaFile> Images,
    IReadOnlyList<string> ImageUrls,
    MediaFile? Video);

/// <summary>Gönderi düzenleme isteği: yalnızca metin alanları ve etiketler değişir.</summary>
public sealed record EditPostCommand(string? Title, string? Body, string? LinkUrl, string? Tags);

public sealed record ProfileUpdate(string BlogName, string DisplayName, string? Bio);
