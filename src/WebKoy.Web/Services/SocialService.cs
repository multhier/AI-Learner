using Microsoft.EntityFrameworkCore;
using WebKoy.Web.Data;

namespace WebKoy.Web.Services;

/// <summary>Takip, beğeni, yorum ve etiket işlemleri.</summary>
public class SocialService(AppDbContext db)
{
    public const int MaxTagsPerSite = 5;

    public async Task<bool> ToggleFollowAsync(string userId, int siteId)
    {
        var f = await db.Follows.FindAsync(userId, siteId);
        if (f is null) db.Follows.Add(new Follow { UserId = userId, SiteId = siteId });
        else db.Follows.Remove(f);
        await db.SaveChangesAsync();
        return f is null;
    }

    public async Task<bool> ToggleLikeAsync(string userId, int siteId)
    {
        var l = await db.Likes.FindAsync(userId, siteId);
        if (l is null) db.Likes.Add(new Like { UserId = userId, SiteId = siteId });
        else db.Likes.Remove(l);
        await db.SaveChangesAsync();
        return l is null;
    }

    public async Task AddCommentAsync(string userId, int siteId, string body)
    {
        body = (body ?? "").Trim();
        if (body.Length == 0) throw new SiteFileException("Yorum boş olamaz.");
        if (body.Length > 1000) throw new SiteFileException("Yorum en fazla 1000 karakter olabilir.");
        db.Comments.Add(new Comment { AuthorId = userId, SiteId = siteId, Body = body });
        await db.SaveChangesAsync();
    }

    /// <summary>Yorumu yazan kişi ya da sitenin sahibi silebilir.</summary>
    public async Task<bool> DeleteCommentAsync(string userId, int commentId, bool isAdmin)
    {
        var c = await db.Comments.Include(x => x.Site).FirstOrDefaultAsync(x => x.Id == commentId);
        if (c is null) return false;
        if (!isAdmin && c.AuthorId != userId && c.Site.OwnerId != userId) return false;
        db.Comments.Remove(c);
        await db.SaveChangesAsync();
        return true;
    }

    public static List<string> ParseTags(string? input) =>
        (input ?? "").Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.TrimStart('#').ToLowerInvariant())
            .Where(t => t.Length is >= 2 and <= 25 && t.All(ch => char.IsLetterOrDigit(ch) || ch == '-'))
            .Distinct()
            .Take(MaxTagsPerSite)
            .ToList();

    public async Task SetTagsAsync(Site site, IEnumerable<string> tagNames)
    {
        var names = tagNames.ToList();
        var existing = await db.Tags.Where(t => names.Contains(t.Name)).ToListAsync();
        foreach (var n in names.Except(existing.Select(t => t.Name)))
        {
            var t = new Tag { Name = n };
            db.Tags.Add(t);
            existing.Add(t);
        }

        var current = await db.SiteTags.Where(st => st.SiteId == site.Id).ToListAsync();
        db.SiteTags.RemoveRange(current);
        foreach (var t in existing) db.SiteTags.Add(new SiteTag { SiteId = site.Id, Tag = t });
        await db.SaveChangesAsync();
    }
}
