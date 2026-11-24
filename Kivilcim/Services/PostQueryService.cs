using Kivilcim.Data;
using Kivilcim.Models;
using Microsoft.EntityFrameworkCore;

namespace Kivilcim.Services;

/// <summary>Gönderi listelerini (akış, blog, etiket, arama) kart modeline çeviren sorgular.</summary>
public class PostQueryService
{
    public const int PageSize = 10;

    private readonly ApplicationDbContext _db;

    public PostQueryService(ApplicationDbContext db) => _db = db;

    /// <summary>
    /// Id'ye göre azalan sırada bir sayfa döner. <paramref name="before"/> verilirse o Id'den
    /// küçük gönderiler gelir (Tumblr'daki max_post_id mantığı).
    /// </summary>
    public async Task<List<PostCardViewModel>> GetPageAsync(
        IQueryable<Post> query, string? currentUserId, int? before, bool applyHiddenTags = true)
    {
        if (before.HasValue)
        {
            query = query.Where(p => p.Id < before.Value);
        }

        if (applyHiddenTags && currentUserId != null)
        {
            var hidden = _db.HiddenTags.Where(h => h.UserId == currentUserId).Select(h => h.TagName);
            // Reblog'larda asıl gönderinin etiketleri de kontrol edilir.
            query = query.Where(p =>
                !p.PostTags.Any(pt => hidden.Contains(pt.Tag.Name)) &&
                (p.ReblogOf == null || !p.ReblogOf.PostTags.Any(pt => hidden.Contains(pt.Tag.Name))));
        }

        return await Project(query.OrderByDescending(p => p.Id).Take(PageSize), currentUserId).ToListAsync();
    }

    public Task<PostCardViewModel?> GetOneAsync(int id, string? currentUserId) =>
        Project(_db.Posts.Where(p => p.Id == id), currentUserId).FirstOrDefaultAsync();

    /// <summary>Son N günde en çok beğeni + reblog alan özgün gönderiler.</summary>
    public async Task<List<PostCardViewModel>> GetPopularAsync(string? currentUserId, int days = 30, int take = 20)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        var query = _db.Posts
            .Where(p => p.ReblogOfId == null && p.CreatedAt >= since)
            .OrderByDescending(p => p.Likes.Count + p.Reblogs.Count)
            .ThenByDescending(p => p.Id)
            .Take(take);
        return await Project(query, currentUserId).ToListAsync();
    }

    public async Task<List<TagCount>> GetTrendingTagsAsync(int days = 30, int take = 12)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        var rows = await _db.PostTags
            .Where(pt => pt.Post.CreatedAt >= since)
            .GroupBy(pt => pt.Tag.Name)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .OrderByDescending(t => t.Count)
            .ThenBy(t => t.Name)
            .Take(take)
            .ToListAsync();
        return rows.Select(r => new TagCount(r.Name, r.Count)).ToList();
    }

    private static IQueryable<PostCardViewModel> Project(IQueryable<Post> query, string? me) =>
        query.Select(p => new PostCardViewModel
        {
            Id = p.Id,
            Type = p.Type,
            Title = p.Title,
            Body = p.Body,
            ImagePath = p.ImagePath,
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
                    ImagePath = p.ReblogOf.ImagePath,
                    VideoPath = p.ReblogOf.VideoPath,
                    LinkUrl = p.ReblogOf.LinkUrl,
                    AuthorBlogName = p.ReblogOf.Author.BlogName!,
                    AuthorDisplayName = p.ReblogOf.Author.DisplayName,
                    AuthorAvatar = p.ReblogOf.Author.AvatarPath
                }
        });
}
