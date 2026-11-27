using Kivilcim.Bll.Abstractions;
using Kivilcim.Bll.Dtos;
using Kivilcim.Bll.Utilities;
using Kivilcim.Core.Abstractions;
using Kivilcim.Model;

namespace Kivilcim.Bll.Services;

/// <summary>Gönderi listeleri ve detayları. Sorgular Unit of Work üzerinden kurulur, çalıştırma IQueryExecutor ile yapılır.</summary>
public sealed class PostQueryService : IPostQueryService
{
    private readonly IUnitOfWork _uow;
    private readonly IQueryExecutor _query;

    public PostQueryService(IUnitOfWork uow, IQueryExecutor query)
    {
        _uow = uow;
        _query = query;
    }

    public int PageSize => 10;

    private IQueryable<Post> Posts => _uow.Repository<Post>().Query();

    public Task<List<PostCardViewModel>> FeedPageAsync(string me, int? before, CancellationToken ct = default)
    {
        var followees = _uow.Repository<Follow>().Query().Where(f => f.FollowerId == me).Select(f => f.FolloweeId);
        var query = Posts.Where(p => p.AuthorId == me || followees.Contains(p.AuthorId));
        return PagedAsync(query, me, before, applyHiddenTags: true, ct);
    }

    public Task<List<PostCardViewModel>> BlogPageAsync(string authorId, string? viewerId, int? before, CancellationToken ct = default) =>
        PagedAsync(Posts.Where(p => p.AuthorId == authorId), viewerId, before, applyHiddenTags: false, ct);

    public Task<List<PostCardViewModel>> TaggedPageAsync(string tag, string? viewerId, int? before, CancellationToken ct = default)
    {
        var name = TagParser.Normalize(tag);
        return PagedAsync(Posts.Where(p => p.PostTags.Any(pt => pt.Tag.Name == name)), viewerId, before, applyHiddenTags: true, ct);
    }

    public Task<List<PostCardViewModel>> SearchPageAsync(string term, string? viewerId, int? before, CancellationToken ct = default)
    {
        var lowered = term.ToLower();
        var tagName = TagParser.Normalize(term);
        var query = Posts.Where(p =>
            (p.Title != null && p.Title.ToLower().Contains(lowered)) ||
            (p.Body != null && p.Body.ToLower().Contains(lowered)) ||
            p.PostTags.Any(pt => pt.Tag.Name == tagName));
        return PagedAsync(query, viewerId, before, applyHiddenTags: true, ct);
    }

    /// <summary>Son 30 günde en çok beğenilen ve paylaşılan özgün gönderiler.</summary>
    public Task<List<PostCardViewModel>> PopularAsync(string? viewerId, CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddDays(-30);
        var query = Posts
            .Where(p => p.ReblogOfId == null && p.CreatedAt >= since)
            .OrderByDescending(p => p.Likes.Count + p.Reblogs.Count)
            .ThenByDescending(p => p.Id)
            .Take(20);
        return _query.ToListAsync(Project(query, viewerId), ct);
    }

    public async Task<List<TagCount>> TrendingTagsAsync(CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddDays(-30);
        var rows = await _query.ToListAsync(_uow.Repository<PostTag>().Query()
            .Where(pt => pt.Post.CreatedAt >= since)
            .GroupBy(pt => pt.Tag.Name)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .OrderByDescending(t => t.Count)
            .ThenBy(t => t.Name)
            .Take(12), ct);
        return rows.Select(r => new TagCount(r.Name, r.Count)).ToList();
    }

    public async Task<PostDetailsViewModel?> GetDetailsAsync(int id, string? viewerId, CancellationToken ct = default)
    {
        var post = await _query.FirstOrDefaultAsync(Project(Posts.Where(p => p.Id == id), viewerId), ct);
        if (post == null)
        {
            return null;
        }

        var replies = await _query.ToListAsync(_uow.Repository<Reply>().Query()
            .Where(r => r.PostId == id)
            .OrderBy(r => r.Id)
            .Select(r => new ReplyViewModel(r.Id, r.User.BlogName!, r.User.DisplayName, r.User.AvatarPath, r.Body, r.CreatedAt)), ct);

        var likes = await _query.ToListAsync(_uow.Repository<Like>().Query()
            .Where(l => l.PostId == id)
            .OrderByDescending(l => l.CreatedAt).Take(20)
            .Select(l => new NoteViewModel("like", l.User.BlogName!, l.CreatedAt)), ct);

        var reblogs = await _query.ToListAsync(Posts
            .Where(p => p.ReblogOfId == id)
            .OrderByDescending(p => p.Id).Take(20)
            .Select(p => new NoteViewModel("reblog", p.Author.BlogName!, p.CreatedAt)), ct);

        return new PostDetailsViewModel
        {
            Post = post,
            Replies = replies,
            RecentNotes = likes.Concat(reblogs).OrderByDescending(n => n.CreatedAt).Take(20).ToList()
        };
    }

    public async Task<PostEditDto?> GetForEditAsync(int id, string me, CancellationToken ct = default)
    {
        var post = await _query.FirstOrDefaultAsync(Posts
            .Where(p => p.Id == id && p.AuthorId == me)
            .Select(p => new PostEditDto
            {
                Id = p.Id,
                Type = p.Type,
                Title = p.Title,
                Body = p.Body,
                LinkUrl = p.LinkUrl
            }), ct);
        if (post == null)
        {
            return null;
        }

        var tags = await _query.ToListAsync(_uow.Repository<PostTag>().Query()
            .Where(pt => pt.PostId == id).Select(pt => pt.Tag.Name), ct);
        var images = await _query.ToListAsync(_uow.Repository<PostImage>().Query()
            .Where(i => i.PostId == id).OrderBy(i => i.SortOrder).Select(i => i.Url), ct);

        post.Tags = string.Join(", ", tags);
        post.Images = images;
        return post;
    }

    private async Task<List<PostCardViewModel>> PagedAsync(
        IQueryable<Post> query, string? viewerId, int? before, bool applyHiddenTags, CancellationToken ct)
    {
        if (before.HasValue)
        {
            query = query.Where(p => p.Id < before.Value);
        }

        if (applyHiddenTags && viewerId != null)
        {
            var hidden = _uow.Repository<HiddenTag>().Query().Where(h => h.UserId == viewerId).Select(h => h.TagName);
            query = query.Where(p =>
                !p.PostTags.Any(pt => hidden.Contains(pt.Tag.Name)) &&
                (p.ReblogOf == null || !p.ReblogOf.PostTags.Any(pt => hidden.Contains(pt.Tag.Name))));
        }

        return await _query.ToListAsync(Project(query.OrderByDescending(p => p.Id).Take(PageSize), viewerId), ct);
    }

    private static IQueryable<PostCardViewModel> Project(IQueryable<Post> query, string? me) =>
        query.Select(p => new PostCardViewModel
        {
            Id = p.Id,
            Type = p.Type,
            Title = p.Title,
            Body = p.Body,
            Images = p.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).ToList(),
            VideoPath = p.VideoPath,
            LinkUrl = p.LinkUrl,
            CreatedAt = p.CreatedAt,
            AuthorId = p.AuthorId,
            AuthorBlogName = p.Author.BlogName!,
            AuthorDisplayName = p.Author.DisplayName,
            AuthorAvatar = p.Author.AvatarPath,
            Tags = p.PostTags.Select(pt => pt.Tag.Name).ToList(),
            LikeCount = p.Likes.Count,
            ReplyCount = p.Replies.Count,
            ReblogCount = p.Reblogs.Count,
            LikedByMe = me != null && p.Likes.Any(l => l.UserId == me),
            Original = p.ReblogOf == null
                ? null
                : new OriginalPostViewModel
                {
                    Id = p.ReblogOf.Id,
                    Type = p.ReblogOf.Type,
                    Title = p.ReblogOf.Title,
                    Body = p.ReblogOf.Body,
                    Images = p.ReblogOf.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).ToList(),
                    VideoPath = p.ReblogOf.VideoPath,
                    LinkUrl = p.ReblogOf.LinkUrl,
                    AuthorBlogName = p.ReblogOf.Author.BlogName!,
                    AuthorDisplayName = p.ReblogOf.Author.DisplayName,
                    AuthorAvatar = p.ReblogOf.Author.AvatarPath
                }
        });
}
