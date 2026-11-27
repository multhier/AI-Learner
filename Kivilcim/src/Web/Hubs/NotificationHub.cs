using Kivilcim.Core.Realtime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Kivilcim.Web.Hubs;

/// <summary>
/// Canlı bağlantı. Sunucu, Core'daki IRealtimeNotifier üzerinden kullanıcıya veya gruba yazar.
/// İstemci bir gönderiyi açınca o gönderinin grubuna katılır; yanıt ve düzenlemeler canlı gelir.
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    public Task JoinPost(int postId) => Groups.AddToGroupAsync(Context.ConnectionId, RealtimeGroups.Post(postId));

    public Task LeavePost(int postId) => Groups.RemoveFromGroupAsync(Context.ConnectionId, RealtimeGroups.Post(postId));
}
