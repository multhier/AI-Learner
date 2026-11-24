using System.Security.Claims;
using Kivilcim.Data;
using Kivilcim.Models;
using Kivilcim.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kivilcim.Controllers;

[Authorize]
public class MessagesController : Controller
{
    public const int MaxLength = 4000;

    private readonly ApplicationDbContext _db;
    private readonly NotificationService _notifications;

    public MessagesController(ApplicationDbContext db, NotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    private string Me => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>Konuşma listesi: her kişiyle son mesaj ve okunmamış sayısı.</summary>
    [HttpGet("/messages")]
    public async Task<IActionResult> Index()
    {
        var me = Me;
        var mine = _db.Messages.Where(m => m.SenderId == me || m.RecipientId == me);
        var lastIds = mine
            .GroupBy(m => m.SenderId == me ? m.RecipientId : m.SenderId)
            .Select(g => g.Max(m => m.Id));

        var rows = await _db.Messages
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
            })
            .ToListAsync();

        var unread = await _db.Messages
            .Where(m => m.RecipientId == me && m.ReadAt == null)
            .GroupBy(m => m.SenderId)
            .Select(g => new { SenderId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SenderId, x => x.Count);

        return View(rows.Select(r => new ConversationViewModel(
            r.BlogName!, r.DisplayName, r.AvatarPath, r.Body, r.FromMe, r.CreatedAt,
            unread.GetValueOrDefault(r.OtherId))).ToList());
    }

    [HttpGet("/messages/{blogName}")]
    public async Task<IActionResult> Thread(string blogName)
    {
        var me = Me;
        var other = await _db.Users.Where(u => u.BlogName == blogName)
            .Select(u => new { u.Id, u.BlogName, u.DisplayName, u.AvatarPath }).FirstOrDefaultAsync();
        if (other == null) return NotFound();
        if (other.Id == me) return RedirectToAction(nameof(Index));

        var messages = await _db.Messages
            .Where(m => (m.SenderId == me && m.RecipientId == other.Id) || (m.SenderId == other.Id && m.RecipientId == me))
            .OrderByDescending(m => m.Id)
            .Take(100)
            .Select(m => new MessageViewModel(m.Id, m.SenderId == me, m.Body, m.CreatedAt))
            .ToListAsync();
        messages.Reverse();

        await _db.Messages
            .Where(m => m.SenderId == other.Id && m.RecipientId == me && m.ReadAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.ReadAt, DateTime.UtcNow));

        return View(new ThreadViewModel
        {
            BlogName = other.BlogName!, DisplayName = other.DisplayName, AvatarPath = other.AvatarPath, Messages = messages
        });
    }

    [HttpPost("/messages/{blogName}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(string blogName, string? body)
    {
        var me = Me;
        var otherId = await _db.Users.Where(u => u.BlogName == blogName).Select(u => u.Id).FirstOrDefaultAsync();
        if (otherId == null) return NotFound();
        if (otherId == me) return BadRequest();

        var text = body?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return IsFetch() ? BadRequest(new { error = "Mesaj boş olamaz." }) : RedirectToAction(nameof(Thread), new { blogName });
        }
        if (text.Length > MaxLength) text = text[..MaxLength];

        var message = new Message { SenderId = me, RecipientId = otherId, Body = text };
        _db.Messages.Add(message);
        await _db.SaveChangesAsync();

        var sender = await _db.Users.Where(u => u.Id == me).Select(u => new { u.BlogName, u.DisplayName }).FirstAsync();
        var unread = await _db.Messages.CountAsync(m => m.RecipientId == otherId && m.ReadAt == null);
        await _notifications.PushMessageAsync(otherId, new
        {
            id = message.Id,
            fromBlog = sender.BlogName,
            fromName = sender.DisplayName,
            body = text,
            time = message.CreatedAt.ToString("o"),
            unread
        });

        if (IsFetch())
        {
            return Json(new { id = message.Id, body = text, time = message.CreatedAt.ToString("o") });
        }
        return RedirectToAction(nameof(Thread), new { blogName });
    }

    /// <summary>Açık sohbet sayfasında gelen mesaj okununca istemci bildirir.</summary>
    [HttpPost("/messages/{blogName}/read")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(string blogName)
    {
        var me = Me;
        var otherId = await _db.Users.Where(u => u.BlogName == blogName).Select(u => u.Id).FirstOrDefaultAsync();
        if (otherId == null) return NotFound();
        await _db.Messages
            .Where(m => m.SenderId == otherId && m.RecipientId == me && m.ReadAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.ReadAt, DateTime.UtcNow));
        return NoContent();
    }

    private bool IsFetch() => Request.Headers["X-Requested-With"] == "fetch";
}
