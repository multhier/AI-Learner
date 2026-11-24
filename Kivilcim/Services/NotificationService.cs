using Kivilcim.Data;
using Kivilcim.Hubs;
using Kivilcim.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Kivilcim.Services;

/// <summary>Bildirim kaydeder ve alıcının açık sayfalarına SignalR ile anında iletir.</summary>
public class NotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly IHubContext<NotificationHub> _hub;

    public NotificationService(ApplicationDbContext db, IHubContext<NotificationHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    public async Task NotifyAsync(string recipientId, string actorId, NotificationType type, int? postId = null)
    {
        if (recipientId == actorId)
        {
            return; // Kendi gönderini beğenmek bildirim üretmez.
        }

        var actor = await _db.Users.Where(u => u.Id == actorId)
            .Select(u => new { u.BlogName, u.DisplayName, u.AvatarPath }).FirstAsync();

        _db.Notifications.Add(new Notification
        {
            RecipientId = recipientId, ActorId = actorId, Type = type, PostId = postId
        });
        await _db.SaveChangesAsync();

        var unread = await _db.Notifications.CountAsync(n => n.RecipientId == recipientId && !n.IsRead);
        await _hub.Clients.User(recipientId).SendAsync("notification", new
        {
            text = $"{actor.BlogName} {Describe(type)}",
            url = postId.HasValue ? $"/post/{postId}" : $"/blog/{actor.BlogName}",
            unread
        });
    }

    /// <summary>Takip bırakılınca ya da beğeni geri alınınca okunmamış ilgili bildirimi siler.</summary>
    public async Task RetractAsync(string recipientId, string actorId, NotificationType type, int? postId = null)
    {
        var items = await _db.Notifications
            .Where(n => n.RecipientId == recipientId && n.ActorId == actorId && n.Type == type && n.PostId == postId && !n.IsRead)
            .ToListAsync();
        if (items.Count == 0) return;
        _db.Notifications.RemoveRange(items);
        await _db.SaveChangesAsync();
    }

    public async Task PushMessageAsync(string recipientId, object payload)
    {
        await _hub.Clients.User(recipientId).SendAsync("message", payload);
    }

    public static string Describe(NotificationType type) => type switch
    {
        NotificationType.Like => "gönderini beğendi",
        NotificationType.Reblog => "gönderini yeniden paylaştı",
        NotificationType.Reply => "gönderine yanıt yazdı",
        NotificationType.Follow => "seni takip etmeye başladı",
        _ => ""
    };
}
