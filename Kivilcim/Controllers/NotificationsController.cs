using System.Security.Claims;
using Kivilcim.Data;
using Kivilcim.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kivilcim.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly ApplicationDbContext _db;

    public NotificationsController(ApplicationDbContext db) => _db = db;

    [HttpGet("/notifications")]
    public async Task<IActionResult> Index()
    {
        var me = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var items = await _db.Notifications
            .Where(n => n.RecipientId == me)
            .OrderByDescending(n => n.Id)
            .Take(50)
            .Select(n => new NotificationViewModel(
                n.Id, n.Type, n.Actor.BlogName!, n.Actor.DisplayName, n.Actor.AvatarPath,
                n.PostId,
                n.Post == null ? null : (n.Post.Title ?? n.Post.Body),
                n.IsRead, n.CreatedAt))
            .ToListAsync();

        // Sayfa açılınca hepsi okundu sayılır (liste okunmadan önce çekildi, vurgular korunur).
        await _db.Notifications
            .Where(n => n.RecipientId == me && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

        return View(items);
    }
}
