using System.Security.Claims;
using Kivilcim.Bll.Dtos;
using Kivilcim.Bll.Media;
using Kivilcim.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace Kivilcim.Web.Controllers;

/// <summary>Ortak controller işleri: oturumdaki kullanıcı, sayfalama bağlantıları, form dosyası çevirisi.</summary>
public abstract class KivilcimController : Controller
{
    protected string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    protected bool IsFetch => Request.Headers["X-Requested-With"] == "fetch";

    /// <summary>Sayfa dolduysa "Daha fazla göster" için bağlantı; dolmadıysa null.</summary>
    protected string? NextPageUrl(IReadOnlyList<PostCardViewModel> cards, int pageSize, string action, string? controller, object routeValues)
    {
        if (cards.Count < pageSize || cards.Count == 0)
        {
            return null;
        }
        return Url.Action(action, controller, routeValues);
    }

    protected IActionResult BackTo(string? returnUrl, int postId) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction("Details", "Posts", new { id = postId });

    protected static MediaFile ToMedia(Microsoft.AspNetCore.Http.IFormFile file) =>
        new(file.FileName, file.Length, file.OpenReadStream);
}
