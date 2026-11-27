using Kivilcim.Bll.Abstractions;
using Kivilcim.Bll.Dtos;
using Kivilcim.Bll.Rules;
using Kivilcim.Core.Abstractions;
using Kivilcim.Core.Results;
using Kivilcim.Model;

namespace Kivilcim.Bll.Services;

public sealed class MessageService : IMessageService
{
    private readonly IUnitOfWork _uow;
    private readonly IQueryExecutor _query;
    private readonly IRealtimeNotifier _realtime;

    public MessageService(IUnitOfWork uow, IQueryExecutor query, IRealtimeNotifier realtime)
    {
        _uow = uow;
        _query = query;
        _realtime = realtime;
    }

    private IQueryable<Message> Messages => _uow.Repository<Message>().Query();

    public async Task<List<ConversationViewModel>> ConversationsAsync(string me, CancellationToken ct = default)
    {
        var mine = Messages.Where(m => m.SenderId == me || m.RecipientId == me);
        var lastIds = mine
            .GroupBy(m => m.SenderId == me ? m.RecipientId : m.SenderId)
            .Select(g => g.Max(m => m.Id));

        var rows = await _query.ToListAsync(Messages
            .Where(m => lastIds.Contains(m.Id))
            .OrderByDescending(m => m.Id)
            .Select(m => new
            {
                OtherId = m.SenderId == me ? m.RecipientId : m.SenderId,
                BlogName = m.SenderId == me ? m.Recipient.BlogName : m.Sender.BlogName,
                DisplayName = m.SenderId == me ? m.Recipient.DisplayName : m.Sender.DisplayName,
                AvatarPath = m.SenderId == me ? m.Recipient.AvatarPath : m.Sender.AvatarPath,
                m.Body,
                FromMe = m.SenderId == me,
                m.CreatedAt
            }), ct);

        var unread = (await _query.ToListAsync(Messages
                .Where(m => m.RecipientId == me && m.ReadAt == null)
                .Select(m => m.SenderId), ct))
            .GroupBy(id => id)
            .ToDictionary(g => g.Key, g => g.Count());

        return rows.Select(r => new ConversationViewModel(
            r.BlogName!, r.DisplayName, r.AvatarPath, r.Body, r.FromMe, r.CreatedAt,
            unread.GetValueOrDefault(r.OtherId))).ToList();
    }

    public async Task<ThreadViewModel?> ThreadAsync(string me, string blogName, CancellationToken ct = default)
    {
        var other = await _query.FirstOrDefaultAsync(_uow.Repository<ApplicationUser>().Query()
            .Where(u => u.BlogName == blogName)
            .Select(u => new { u.Id, u.BlogName, u.DisplayName, u.AvatarPath }), ct);
        if (other == null || other.Id == me)
        {
            return null;
        }

        var messages = await _query.ToListAsync(Messages
            .Where(m => (m.SenderId == me && m.RecipientId == other.Id) || (m.SenderId == other.Id && m.RecipientId == me))
            .OrderByDescending(m => m.Id)
            .Take(100)
            .Select(m => new MessageViewModel(m.Id, m.SenderId == me, m.Body, m.CreatedAt)), ct);
        messages.Reverse();

        await MarkReadInternalAsync(other.Id, me, ct);

        return new ThreadViewModel
        {
            BlogName = other.BlogName!,
            DisplayName = other.DisplayName,
            AvatarPath = other.AvatarPath,
            Messages = messages
        };
    }

    public async Task<Result<MessageViewModel>> SendAsync(string me, string blogName, string? body, CancellationToken ct = default)
    {
        var recipientId = await _query.FirstOrDefaultAsync(_uow.Repository<ApplicationUser>().Query()
            .Where(u => u.BlogName == blogName).Select(u => u.Id), ct);
        if (recipientId == null)
        {
            return Result.Failure<MessageViewModel>("Kullanıcı bulunamadı.");
        }
        if (recipientId == me)
        {
            return Result.Failure<MessageViewModel>("Kendine mesaj gönderemezsin.");
        }

        var text = body?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return Result.Failure<MessageViewModel>("Mesaj boş olamaz.");
        }
        if (text.Length > PostRules.MaxMessageLength)
        {
            text = text[..PostRules.MaxMessageLength];
        }

        var message = new Message { SenderId = me, RecipientId = recipientId, Body = text };
        await _uow.Repository<Message>().AddAsync(message, ct);
        await _uow.SaveChangesAsync(ct);

        var sender = await _query.FirstOrDefaultAsync(_uow.Repository<ApplicationUser>().Query()
            .Where(u => u.Id == me).Select(u => new { u.BlogName, u.DisplayName }), ct);
        var unread = await _query.CountAsync(Messages.Where(m => m.RecipientId == recipientId && m.ReadAt == null), ct);

        await _realtime.SendToUserAsync(recipientId, "message", new
        {
            id = message.Id,
            fromBlog = sender?.BlogName,
            fromName = sender?.DisplayName,
            body = text,
            time = message.CreatedAt.ToString("o"),
            unread
        }, ct);

        return Result.Success(new MessageViewModel(message.Id, true, text, message.CreatedAt));
    }

    public async Task<bool> MarkReadAsync(string me, string blogName, CancellationToken ct = default)
    {
        var otherId = await _query.FirstOrDefaultAsync(_uow.Repository<ApplicationUser>().Query()
            .Where(u => u.BlogName == blogName).Select(u => u.Id), ct);
        if (otherId == null)
        {
            return false;
        }
        await MarkReadInternalAsync(otherId, me, ct);
        return true;
    }

    private async Task MarkReadInternalAsync(string senderId, string recipientId, CancellationToken ct)
    {
        var unread = await _query.ToListAsync(Messages
            .Where(m => m.SenderId == senderId && m.RecipientId == recipientId && m.ReadAt == null), ct);
        if (unread.Count == 0)
        {
            return;
        }
        foreach (var m in unread)
        {
            m.ReadAt = DateTime.UtcNow;
        }
        await _uow.SaveChangesAsync(ct);
    }
}
