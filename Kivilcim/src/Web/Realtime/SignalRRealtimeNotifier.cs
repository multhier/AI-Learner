using Kivilcim.Core.Abstractions;
using Kivilcim.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Kivilcim.Web.Realtime;

/// <summary>IRealtimeNotifier'ın SignalR gerçeklemesi. Uygulama katmanı SignalR'ı bilmez.</summary>
public sealed class SignalRRealtimeNotifier : IRealtimeNotifier
{
    private readonly IHubContext<NotificationHub> _hub;

    public SignalRRealtimeNotifier(IHubContext<NotificationHub> hub) => _hub = hub;

    public Task SendToUserAsync(string userId, string method, object payload, CancellationToken ct = default) =>
        _hub.Clients.User(userId).SendAsync(method, payload, ct);

    public Task SendToGroupAsync(string group, string method, object payload, CancellationToken ct = default) =>
        _hub.Clients.Group(group).SendAsync(method, payload, ct);
}
