using Kivilcim.Bll.Dtos;
using Kivilcim.Model;

namespace Kivilcim.Bll.Abstractions;

public interface INotificationService
{
    Task NotifyAsync(string recipientId, string actorId, NotificationType type, int? postId, CancellationToken ct = default);

    Task RetractAsync(string recipientId, string actorId, NotificationType type, int? postId, CancellationToken ct = default);

    /// <summary>Bildirimleri döner ve listelenenleri okundu işaretler.</summary>
    Task<List<NotificationViewModel>> ListAndMarkReadAsync(string me, CancellationToken ct = default);
}
