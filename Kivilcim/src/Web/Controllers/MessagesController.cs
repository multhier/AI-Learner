using Kivilcim.Bll.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kivilcim.Web.Controllers;

[Authorize]
public class MessagesController : KivilcimController
{
    private readonly IMessageService _messages;
    private readonly IProfileService _profiles;

    public MessagesController(IMessageService messages, IProfileService profiles)
    {
        _messages = messages;
        _profiles = profiles;
    }

    [HttpGet("/messages")]
    public async Task<IActionResult> Index()
    {
        return View(await _messages.ConversationsAsync(CurrentUserId!));
    }

    [HttpGet("/messages/{blogName}")]
    public async Task<IActionResult> Thread(string blogName)
    {
        // Kendi blogunla sohbet yoktur; mesaj listesine dön.
        var me = await _profiles.GetProfileAsync(CurrentUserId!);
        if (me != null && me.BlogName == blogName)
        {
            return RedirectToAction(nameof(Index));
        }

        var thread = await _messages.ThreadAsync(CurrentUserId!, blogName);
        return thread == null ? NotFound() : View(thread);
    }

    [HttpPost("/messages/{blogName}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(string blogName, string? body)
    {
        var result = await _messages.SendAsync(CurrentUserId!, blogName, body);
        if (!result.Succeeded && result.Errors.Any(e => e.Message.Contains("bulunamadı")))
        {
            return NotFound();
        }

        // Sayfa içi (fetch) gönderimde JSON, normal form gönderiminde konuşma sayfasına dönüş.
        if (IsFetch)
        {
            return result.Succeeded ? Json(result.Value) : BadRequest(new { error = result.FirstError });
        }
        return RedirectToAction(nameof(Thread), new { blogName });
    }

    [HttpPost("/messages/{blogName}/read")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(string blogName)
    {
        return await _messages.MarkReadAsync(CurrentUserId!, blogName) ? NoContent() : NotFound();
    }
}
