using Kivilcim.Bll.Dtos;
using Kivilcim.Core.Results;

namespace Kivilcim.Bll.Abstractions;

public interface IMessageService
{
    Task<List<ConversationViewModel>> ConversationsAsync(string me, CancellationToken ct = default);

    Task<ThreadViewModel?> ThreadAsync(string me, string blogName, CancellationToken ct = default);

    Task<Result<MessageViewModel>> SendAsync(string me, string blogName, string? body, CancellationToken ct = default);

    Task<bool> MarkReadAsync(string me, string blogName, CancellationToken ct = default);
}
