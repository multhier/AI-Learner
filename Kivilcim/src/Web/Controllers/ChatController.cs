using Kivilcim.Bll.Abstractions;
using Kivilcim.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kivilcim.Web.Controllers;

/// <summary>Sağ alttaki konuşma paneli için JSON uçları. Kurallar Bll'de, burada yalnızca taşıma.</summary>
[Authorize]
[Route("api/chat")]
public class ChatController : KivilcimController
{
    private readonly IMessageService _messages;

    public ChatController(IMessageService messages) => _messages = messages;

    [HttpGet("conversations")]
    public async Task<IActionResult> Conversations() =>
        Json(await _messages.ConversationsAsync(CurrentUserId!));

    [HttpGet("{blogName}")]
    public async Task<IActionResult> Thread(string blogName)
    {
        var thread = await _messages.ThreadAsync(CurrentUserId!, blogName);
        return thread == null ? NotFound() : Json(thread);
    }

    [HttpPost("{blogName}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(string blogName, [FromBody] ChatSendRequest request)
    {
        var result = await _messages.SendAsync(CurrentUserId!, blogName, request.Body);
        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.FirstError });
        }
        return Json(result.Value);
    }

    [HttpPost("{blogName}/read")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(string blogName) =>
        await _messages.MarkReadAsync(CurrentUserId!, blogName) ? NoContent() : NotFound();
}
