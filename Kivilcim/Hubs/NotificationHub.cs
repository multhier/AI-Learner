using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Kivilcim.Hubs;

/// <summary>
/// Girişli kullanıcıların tarayıcısına anlık bildirim ve mesaj gönderen SignalR hub'ı.
/// Sunucu Clients.User(userId) ile yazar; istemci yalnızca dinler.
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
}
