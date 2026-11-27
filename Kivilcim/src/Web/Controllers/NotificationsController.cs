using Kivilcim.Bll.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kivilcim.Web.Controllers;

[Authorize]
public class NotificationsController : KivilcimController
{
    private readonly INotificationService _notifications;

    public NotificationsController(INotificationService notifications) => _notifications = notifications;

    [HttpGet("/notifications")]
    public async Task<IActionResult> Index()
    {
        var items = await _notifications.ListAndMarkReadAsync(CurrentUserId!);
        return View(items);
    }
}
