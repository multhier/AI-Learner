using Kivilcim.Bll.Abstractions;
using Kivilcim.Bll.Dtos;
using Kivilcim.Core.Abstractions;
using Kivilcim.Model;

namespace Kivilcim.Bll.Services;

public sealed class InboxService : IInboxService
{
    private readonly IUnitOfWork _uow;
    private readonly IQueryExecutor _query;

    public InboxService(IUnitOfWork uow, IQueryExecutor query)
    {
        _uow = uow;
        _query = query;
    }

    public async Task<InboxCounts> GetCountsAsync(string me, CancellationToken ct = default)
    {
        var notifications = await _query.CountAsync(_uow.Repository<Notification>().Query()
            .Where(n => n.RecipientId == me && !n.IsRead), ct);
        var messages = await _query.CountAsync(_uow.Repository<Message>().Query()
            .Where(m => m.RecipientId == me && m.ReadAt == null), ct);
        return new InboxCounts(notifications, messages);
    }
}
