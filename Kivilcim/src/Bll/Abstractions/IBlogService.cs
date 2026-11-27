using Kivilcim.Bll.Dtos;

namespace Kivilcim.Bll.Abstractions;

public interface IBlogService
{
    Task<BlogViewModel?> GetBlogAsync(string blogName, string? viewerId, int? before, CancellationToken ct = default);

    /// <summary>Blog bulunamazsa null; kendi blogunu takip edemez.</summary>
    Task<FollowResult?> ToggleFollowAsync(string me, string blogName, CancellationToken ct = default);

    Task<List<BlogSuggestion>> SuggestionsAsync(string me, int take = 5, CancellationToken ct = default);
}
