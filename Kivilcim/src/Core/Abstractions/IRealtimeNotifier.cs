namespace Kivilcim.Core.Abstractions;

/// <summary>Bağlı tarayıcılara anlık mesaj gönderir. Gerçekleme (SignalR) sunum katmanındadır.</summary>
public interface IRealtimeNotifier
{
    Task SendToUserAsync(string userId, string method, object payload, CancellationToken ct = default);

    Task SendToGroupAsync(string group, string method, object payload, CancellationToken ct = default);
}
