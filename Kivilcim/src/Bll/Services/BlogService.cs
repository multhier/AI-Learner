using Kivilcim.Bll.Abstractions;
using Kivilcim.Bll.Dtos;
using Kivilcim.Core.Abstractions;
using Kivilcim.Model;

namespace Kivilcim.Bll.Services;

public sealed class BlogService : IBlogService
{
    private readonly IUnitOfWork _uow;
    private readonly IQueryExecutor _query;
    private readonly IPostQueryService _posts;
    private readonly INotificationService _notifications;

    public BlogService(IUnitOfWork uow, IQueryExecutor query, IPostQueryService posts, INotificationService notifications)
    {
        _uow = uow;
        _query = query;
        _posts = posts;
        _notifications = notifications;
    }

    private IRepository<ApplicationUser> Users => _uow.Repository<ApplicationUser>();

    public async Task<BlogViewModel?> GetBlogAsync(string blogName, string? viewerId, int? before, CancellationToken ct = default)
    {
        var blog = await _query.FirstOrDefaultAsync(Users.Query()
            .Where(u => u.BlogName == blogName)
            .Select(u => new
            {
                u.Id,
                u.BlogName,
                u.DisplayName,
                u.Bio,
                u.AvatarPath,
                u.CreatedAt,
                PostCount = u.Posts.Count,
                FollowerCount = u.Followers.Count,
                FollowingCount = u.Following.Count,
                IsFollowing = viewerId != null && u.Followers.Any(f => f.FollowerId == viewerId)
            }), ct);
        if (blog == null)
        {
            return null;
        }

        // Blog sayfasında gizli etiket filtresi uygulanmaz: ziyaretçi bu blogu bilerek açtı.
        var cards = await _posts.BlogPageAsync(blog.Id, viewerId, before, ct);

        return new BlogViewModel
        {
            BlogName = blog.BlogName!,
            DisplayName = blog.DisplayName,
            Bio = blog.Bio,
            AvatarPath = blog.AvatarPath,
            JoinedAt = blog.CreatedAt,
            PostCount = blog.PostCount,
            FollowerCount = blog.FollowerCount,
            FollowingCount = blog.FollowingCount,
            IsMe = blog.Id == viewerId,
            IsFollowing = blog.IsFollowing,
            Posts = new PostListViewModel { Posts = cards }
        };
    }

    public async Task<FollowResult?> ToggleFollowAsync(string me, string blogName, CancellationToken ct = default)
    {
        var targetId = await _query.FirstOrDefaultAsync(Users.Query().Where(u => u.BlogName == blogName).Select(u => u.Id), ct);
        if (targetId == null)
        {
            return null;
        }
        if (targetId == me)
        {
            return new FollowResult(Following: false, IsSelf: true);
        }

        var existing = await _uow.Repository<Follow>().GetByIdAsync(me, targetId);
        if (existing == null)
        {
            await _uow.Repository<Follow>().AddAsync(new Follow { FollowerId = me, FolloweeId = targetId }, ct);
        }
        else
        {
            _uow.Repository<Follow>().Remove(existing);
        }
        await _uow.SaveChangesAsync(ct);

        if (existing == null)
        {
            await _notifications.NotifyAsync(targetId, me, NotificationType.Follow, null, ct);
        }
        else
        {
            await _notifications.RetractAsync(targetId, me, NotificationType.Follow, null, ct);
        }
        return new FollowResult(Following: existing == null, IsSelf: false);
    }

    public Task<List<BlogSuggestion>> SuggestionsAsync(string me, int take = 5, CancellationToken ct = default)
    {
        var followed = _uow.Repository<Follow>().Query().Where(f => f.FollowerId == me).Select(f => f.FolloweeId);
        var query = Users.Query()
            .Where(u => u.Id != me && !followed.Contains(u.Id))
            .OrderByDescending(u => u.Posts.Count)
            .Take(take)
            .Select(u => new BlogSuggestion(u.BlogName!, u.DisplayName, u.AvatarPath, u.Bio));
        return _query.ToListAsync(query, ct);
    }
}
