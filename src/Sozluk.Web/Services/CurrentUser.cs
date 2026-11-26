using System.Security.Claims;
using Sozluk.Web.Models;

namespace Sozluk.Web.Services;

public static class CurrentUser
{
    public static int? Id(this ClaimsPrincipal p) =>
        int.TryParse(p.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public static string? Name(this ClaimsPrincipal p) => p.Identity?.Name;

    public static UserRole Role(this ClaimsPrincipal p) =>
        Enum.TryParse<UserRole>(p.FindFirstValue(ClaimTypes.Role), out var r) ? r : UserRole.Caylak;

    public static bool IsModerator(this ClaimsPrincipal p) =>
        p.Identity?.IsAuthenticated == true && p.Role() >= UserRole.Moderator;
}
