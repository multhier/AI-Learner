using Kivilcim.Bll.Dtos;
using Kivilcim.Core.Results;

namespace Kivilcim.Bll.Abstractions;

/// <summary>Gönderi oluşturma, düzenleme, silme ve etkileşimler (yazma tarafı).</summary>
public interface IPostService
{
    Task<Result<int>> CreateAsync(string me, CreatePostCommand command, CancellationToken ct = default);

    Task<Result> UpdateAsync(string me, int postId, EditPostCommand command, CancellationToken ct = default);

    Task<Result> DeleteAsync(string me, int postId, CancellationToken ct = default);

    Task<LikeResult?> ToggleLikeAsync(string me, int postId, CancellationToken ct = default);

    Task<Result<int>> ReblogAsync(string me, int sourceId, string? comment, string? tags, CancellationToken ct = default);

    Task<Result<ReplyViewModel>> ReplyAsync(string me, int postId, string? body, CancellationToken ct = default);
}
