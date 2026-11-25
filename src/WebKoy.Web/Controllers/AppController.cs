using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebKoy.Web.Data;
using WebKoy.Web.Models;
using WebKoy.Web.Services;

namespace WebKoy.Web.Controllers;

public abstract class AppController : Controller
{
    protected string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    protected void Flash(string message, bool error = false) =>
        TempData[error ? "Error" : "Success"] = message;

    protected static IQueryable<SiteCardViewModel> ToCards(IQueryable<Site> q, PlatformOptions opt) =>
        q.Select(s => new SiteCardViewModel
        {
            Name = s.Name,
            Title = s.Title,
            Description = s.Description,
            Views = s.Views,
            Hits = s.Hits,
            Followers = s.Followers.Count,
            Likes = s.Likes.Count,
            UpdatedAt = s.UpdatedAt,
            Tags = s.Tags.Select(t => t.Tag.Name).ToList(),
        });

    protected static List<SiteCardViewModel> WithUrls(List<SiteCardViewModel> cards, PlatformOptions opt)
    {
        foreach (var c in cards) c.Url = opt.SiteUrl(c.Name);
        return cards;
    }
}
