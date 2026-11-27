using Kivilcim.Bll.Dtos;

namespace Kivilcim.Bll.Abstractions;

/// <summary>Gönderi listeleri ve detayları (okuma tarafı).</summary>
public interface IPostQueryService
{
    int PageSize { get; }

    Task<List<PostCardViewModel>> FeedPageAsync(string me, int? before, CancellationToken ct = default);

    Task<List<PostCardViewModel>> BlogPageAsync(string authorId, string? viewerId, int? before, CancellationToken ct = default);

    Task<List<PostCardViewModel>> TaggedPageAsync(string tag, string? viewerId, int? before, CancellationToken ct = default);

    Task<List<PostCardViewModel>> SearchPageAsync(string term, string? viewerId, int? before, CancellationToken ct = default);

    Task<List<PostCardViewModel>> PopularAsync(string? viewerId, CancellationToken ct = default);

    Task<List<TagCount>> TrendingTagsAsync(CancellationToken ct = default);

    Task<PostDetailsViewModel?> GetDetailsAsync(int id, string? viewerId, CancellationToken ct = default);

    Task<PostEditDto?> GetForEditAsync(int id, string me, CancellationToken ct = default);
}
