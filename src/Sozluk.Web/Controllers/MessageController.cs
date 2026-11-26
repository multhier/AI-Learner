using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sozluk.Web.Data;
using Sozluk.Web.Models;
using Sozluk.Web.Services;

namespace Sozluk.Web.Controllers;

[Authorize]
public class MessageController(SozlukDbContext db) : Controller
{
    [HttpGet("/mesaj")]
    public async Task<IActionResult> Index()
    {
        var uid = User.Id()!.Value;
        var msgs = await db.Messages
            .Where(m => m.SenderId == uid || m.ReceiverId == uid)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new
            {
                OtherName = m.SenderId == uid ? m.Receiver!.UserName : m.Sender!.UserName,
                m.Body, m.CreatedAt,
                Unread = m.ReceiverId == uid && !m.IsRead
            })
            .Take(500).ToListAsync();

        var convs = msgs.GroupBy(m => m.OtherName)
            .Select(g => new ConversationItem(g.Key, g.First().Body, g.First().CreatedAt, g.Count(x => x.Unread)))
            .ToList();
        return View(convs);
    }

    [HttpGet("/mesaj/{nick}")]
    public async Task<IActionResult> Thread(string nick)
    {
        var uid = User.Id()!.Value;
        var other = await db.Users.FirstOrDefaultAsync(u => u.UserName == nick);
        if (other == null) return NotFound();

        var msgs = await db.Messages
            .Where(m => (m.SenderId == uid && m.ReceiverId == other.Id) || (m.SenderId == other.Id && m.ReceiverId == uid))
            .OrderBy(m => m.CreatedAt).ToListAsync();

        foreach (var m in msgs.Where(m => m.ReceiverId == uid && !m.IsRead)) m.IsRead = true;
        await db.SaveChangesAsync();

        return View(new MessageThreadViewModel { Other = other, Messages = msgs });
    }

    [HttpPost("/mesaj/{nick}")]
    public async Task<IActionResult> Send(string nick, string body)
    {
        var uid = User.Id()!.Value;
        var other = await db.Users.FirstOrDefaultAsync(u => u.UserName == nick);
        if (other == null) return NotFound();
        body = (body ?? "").Trim();
        if (body.Length == 0 || other.Id == uid) return Redirect($"/mesaj/{Uri.EscapeDataString(nick)}");
        if (body.Length > 5000) body = body[..5000];

        db.Messages.Add(new Message { SenderId = uid, ReceiverId = other.Id, Body = body });
        await db.SaveChangesAsync();
        return Redirect($"/mesaj/{Uri.EscapeDataString(nick)}");
    }
}
