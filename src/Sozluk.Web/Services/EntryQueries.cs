using Microsoft.EntityFrameworkCore;
using Sozluk.Web.Data;
using Sozluk.Web.Models;

namespace Sozluk.Web.Services;

public static class EntryQueries
{
    /// <summary>Entry sorgusunu, oturumdaki kullanıcının oy/favori bilgisiyle EntryView'a projekte eder.</summary>
    public static IQueryable<EntryView> ToViews(this IQueryable<Entry> q, SozlukDbContext db, int? userId) =>
        q.Select(e => new EntryView(
            e.Id, e.TopicId, e.Topic!.Title, e.Topic.Slug,
            e.Author!.UserName, e.Author.Role, e.Content,
            e.CreatedAt, e.UpdatedAt,
            e.UpVotes, e.DownVotes, e.FavoriteCount,
            userId == null ? (short)0 :
                db.Votes.Where(v => v.EntryId == e.Id && v.UserId == userId).Select(v => v.Value).FirstOrDefault(),
            userId != null && db.Favorites.Any(f => f.EntryId == e.Id && f.UserId == userId),
            userId != null && e.AuthorId == userId,
            null));

    public static IQueryable<Entry> Visible(this IQueryable<Entry> q) =>
        q.Where(e => !e.IsDeleted && !e.Author!.IsBanned);
}
