using Kivilcim.Bll.Dtos;
using Kivilcim.Bll.Media;
using Kivilcim.Core.Results;

namespace Kivilcim.Bll.Abstractions;

public interface IProfileService
{
    Task<ProfileDto?> GetProfileAsync(string me, CancellationToken ct = default);

    Task<Result> UpdateProfileAsync(string me, ProfileUpdate update, MediaFile? avatar, CancellationToken ct = default);

    Task<Result> AddHiddenTagAsync(string me, string? tag, CancellationToken ct = default);

    Task<Result> RemoveHiddenTagAsync(string me, string tag, CancellationToken ct = default);

    /// <summary>Hesap silinmeden önce diskteki dosyaların yollarını toplar; silme işini çağıran yapar.</summary>
    Task<IReadOnlyList<string>> CollectAccountFilesAsync(string me, CancellationToken ct = default);
}
