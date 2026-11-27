using Kivilcim.Bll.Abstractions;
using Kivilcim.Bll.Dtos;
using Kivilcim.Bll.Utilities;
using Kivilcim.Core.Abstractions;
using Kivilcim.Core.Realtime;
using Kivilcim.Model;

namespace Kivilcim.Bll.Services;

public sealed class NotificationService : INotificationService
{
    private readonly IUnitOfWork _uow;
    private readonly IQueryExecutor _query;
    private readonly IRealtimeNotifier _realtime;

    public NotificationService(IUnitOfWork uow, IQueryExecutor query, IRealtimeNotifier realtime)
    {
        _uow = uow;
        _query = query;
        _realtime = realtime;
    }

    public async Task NotifyAsync(string recipientId, string actorId, NotificationType type, int? postId, CancellationToken ct = default)
    {
        if (recipientId == actorId)
        {
            return; // Kendi eylemin için bildirim üretilmez.
        }

        var actor = await _query.FirstOrDefaultAsync(_uow.Repository<ApplicationUser>().Query()
            .Where(u => u.Id == actorId)
            .Select(u => new { u.BlogName, u.DisplayName, u.AvatarPath }), ct);
        if (actor == null)
        {
            return;
        }

        await _uow.Repository<Notification>().AddAsync(new Notification
        {
            RecipientId = recipientId,
            ActorId = actorId,
            Type = type,
            PostId = postId
        }, ct);
        await _uow.SaveChangesAsync(ct);

        var unread = await _query.CountAsync(_uow.Repository<Notification>().Query()
            .Where(n => n.RecipientId == recipientId && !n.IsRead), ct);

        await _realtime.SendToUserAsync(recipientId, "notification", new
        {
            text = $"{actor.BlogName} {NotificationTexts.Describe(type)}",
            url = postId.HasValue ? $"/post/{postId}" : $"/blog/{actor.BlogName}",
            unread
        }, ct);
    }

    /// <summary>Beğeni geri alınınca ya da takip bırakılınca okunmamış ilgili bildirim silinir.</summary>
    public async Task RetractAsync(string recipientId, string actorId, NotificationType type, int? postId, CancellationToken ct = default)
    {
        var items = await _query.ToListAsync(_uow.Repository<Notification>().Query()
            .Where(n => n.RecipientId == recipientId && n.ActorId == actorId && n.Type == type && n.PostId == postId && !n.IsRead), ct);
        if (items.Count == 0)
        {
            return;
        }
        _uow.Repository<Notification>().RemoveRange(items);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task<List<NotificationViewModel>> ListAndMarkReadAsync(string me, CancellationToken ct = default)
    {
        var list = await _query.ToListAsync(_uow.Repository<Notification>().Query()
            .Where(n => n.RecipientId == me)
            .OrderByDescending(n => n.Id)
            .Take(50)
            .Select(n => new NotificationViewModel(
                n.Id, n.Type, n.Actor.BlogName!, n.Actor.DisplayName, n.Actor.AvatarPath,
                n.PostId,
                n.Post == null ? null : (n.Post.Title ?? n.Post.Body),
                n.IsRead, n.CreatedAt)), ct);

        var unread = await _query.ToListAsync(_uow.Repository<Notification>().Query()
            .Where(n => n.RecipientId == me && !n.IsRead).Take(50), ct);
        if (unread.Count > 0)
        {
            foreach (var n in unread)
            {
                n.IsRead = true;
            }
            await _uow.SaveChangesAsync(ct);
        }
        return list;
    }
}
