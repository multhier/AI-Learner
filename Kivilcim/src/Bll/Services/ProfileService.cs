using Kivilcim.Bll.Abstractions;
using Kivilcim.Bll.Dtos;
using Kivilcim.Bll.Media;
using Kivilcim.Bll.Utilities;
using Kivilcim.Core.Abstractions;
using Kivilcim.Core.Results;
using Kivilcim.Model;

namespace Kivilcim.Bll.Services;

public sealed class ProfileService : IProfileService
{
    private readonly IUnitOfWork _uow;
    private readonly IQueryExecutor _query;
    private readonly IFileStorage _files;

    public ProfileService(IUnitOfWork uow, IQueryExecutor query, IFileStorage files)
    {
        _uow = uow;
        _query = query;
        _files = files;
    }

    private IRepository<ApplicationUser> Users => _uow.Repository<ApplicationUser>();

    public async Task<ProfileDto?> GetProfileAsync(string me, CancellationToken ct = default)
    {
        var user = await _query.FirstOrDefaultAsync(Users.Query()
            .Where(u => u.Id == me)
            .Select(u => new { u.BlogName, u.DisplayName, u.Bio, u.AvatarPath }), ct);
        if (user == null)
        {
            return null;
        }

        var hidden = await _query.ToListAsync(_uow.Repository<HiddenTag>().Query()
            .Where(h => h.UserId == me).OrderBy(h => h.TagName).Select(h => h.TagName), ct);
        return new ProfileDto(user.BlogName ?? "", user.DisplayName, user.Bio, user.AvatarPath, hidden);
    }

    public async Task<Result> UpdateProfileAsync(string me, ProfileUpdate update, MediaFile? avatar, CancellationToken ct = default)
    {
        var user = await Users.GetByIdAsync(me);
        if (user == null)
        {
            return Result.Failure("Kullanıcı bulunamadı.");
        }

        var blogName = update.BlogName.Trim().ToLowerInvariant();
        var errors = new List<ValidationError>();
        if (!BlogNames.ValidPattern.IsMatch(blogName))
        {
            errors.Add(new("BlogName", "3-32 karakter; küçük harf, rakam ve tire kullanın. Tire ile başlayıp bitemez."));
        }
        else if (await _query.AnyAsync(Users.Query().Where(u => u.BlogName == blogName && u.Id != me), ct))
        {
            errors.Add(new("BlogName", "Bu blog adı alınmış."));
        }

        if (string.IsNullOrWhiteSpace(update.DisplayName))
        {
            errors.Add(new("DisplayName", "Görünen ad zorunlu."));
        }

        string? avatarPath = null;
        if (avatar is { Length: > 0 })
        {
            var error = MediaValidator.ValidateImage(avatar);
            if (error != null)
            {
                errors.Add(new("Avatar", error));
            }
        }

        if (errors.Count > 0)
        {
            return Result.Failure(errors);
        }

        if (avatar is { Length: > 0 })
        {
            await using var stream = avatar.OpenRead();
            avatarPath = await _files.SaveAsync(stream, "avatars", MediaValidator.Extension(avatar.FileName), ct);
        }

        var oldAvatar = user.AvatarPath;
        user.BlogName = blogName;
        user.DisplayName = update.DisplayName.Trim();
        user.Bio = string.IsNullOrWhiteSpace(update.Bio) ? null : update.Bio.Trim();
        if (avatarPath != null)
        {
            user.AvatarPath = avatarPath;
        }
        await _uow.SaveChangesAsync(ct);

        if (avatarPath != null)
        {
            _files.Delete(oldAvatar);
        }
        return Result.Success();
    }

    public async Task<Result> AddHiddenTagAsync(string me, string? tag, CancellationToken ct = default)
    {
        var name = TagParser.Normalize(tag ?? "");
        if (name.Length == 0)
        {
            return Result.Failure("Etiket boş olamaz.");
        }
        if (await _query.AnyAsync(_uow.Repository<HiddenTag>().Query().Where(h => h.UserId == me && h.TagName == name), ct))
        {
            return Result.Success();
        }

        await _uow.Repository<HiddenTag>().AddAsync(new HiddenTag { UserId = me, TagName = name }, ct);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> RemoveHiddenTagAsync(string me, string tag, CancellationToken ct = default)
    {
        var item = await _uow.Repository<HiddenTag>().GetByIdAsync(me, tag);
        if (item != null)
        {
            _uow.Repository<HiddenTag>().Remove(item);
            await _uow.SaveChangesAsync(ct);
        }
        return Result.Success();
    }

    public async Task<IReadOnlyList<string>> CollectAccountFilesAsync(string me, CancellationToken ct = default)
    {
        var paths = new List<string>();
        paths.AddRange(await _query.ToListAsync(_uow.Repository<PostImage>().Query()
            .Where(i => i.Post.AuthorId == me && !i.IsExternal).Select(i => i.Url), ct));
        paths.AddRange(await _query.ToListAsync(_uow.Repository<Post>().Query()
            .Where(p => p.AuthorId == me && p.VideoPath != null).Select(p => p.VideoPath!), ct));

        var avatar = await _query.FirstOrDefaultAsync(Users.Query().Where(u => u.Id == me).Select(u => u.AvatarPath), ct);
        if (avatar != null)
        {
            paths.Add(avatar);
        }
        return paths;
    }
}
