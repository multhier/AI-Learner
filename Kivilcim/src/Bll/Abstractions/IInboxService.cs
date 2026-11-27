using Kivilcim.Bll.Dtos;

namespace Kivilcim.Bll.Abstractions;

public interface IInboxService
{
    /// <summary>Üst menüdeki bildirim ve mesaj sayaçları.</summary>
    Task<InboxCounts> GetCountsAsync(string me, CancellationToken ct = default);
}
