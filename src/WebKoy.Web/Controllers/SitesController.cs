using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebKoy.Web.Data;
using WebKoy.Web.Models;
using WebKoy.Web.Services;

namespace WebKoy.Web.Controllers;

/// <summary>Panel tarafındaki herkese açık site profili: istatistik, takip, beğeni, yorum, şikayet.</summary>
[Route("site/{name}/[action]")]
public class SitesController(AppDbContext db, SocialService social, IOptions<PlatformOptions> options) : AppController
{
    private readonly PlatformOptions _opt = options.Value;

    private Task<Site?> FindAsync(string name)
    {
        var n = SiteNameRules.Normalize(name);
        return db.Sites.FirstOrDefaultAsync(s => s.Name == n);
    }

    [HttpGet("/site/{name}")]
    public async Task<IActionResult> Profile(string name)
    {
        var site = await FindAsync(name);
        if (site is null || (site.IsBanned && !User.IsInRole(DbInitializer.AdminRole))) return NotFound();
        var uid = UserId;
        var isAdmin = User.IsInRole(DbInitializer.AdminRole);

        var comments = await db.Comments.Where(c => c.SiteId == site.Id)
            .OrderByDescending(c => c.CreatedAt).Take(100)
            .Select(c => new CommentViewModel
            {
                Id = c.Id,
                Body = c.Body,
                CreatedAt = c.CreatedAt,
                AuthorSite = c.Author.Site != null ? c.Author.Site.Name : c.Author.UserName!,
                CanDelete = isAdmin || c.AuthorId == uid || site.OwnerId == uid,
            }).ToListAsync();

        return View(new SiteProfileViewModel
        {
            Site = site,
            Url = _opt.SiteUrl(site.Name),
            Followers = await db.Follows.CountAsync(f => f.SiteId == site.Id),
            Likes = await db.Likes.CountAsync(l => l.SiteId == site.Id),
            IsFollowing = uid != null && await db.Follows.AnyAsync(f => f.SiteId == site.Id && f.UserId == uid),
            IsLiked = uid != null && await db.Likes.AnyAsync(l => l.SiteId == site.Id && l.UserId == uid),
            IsOwner = uid == site.OwnerId,
            Tags = await db.SiteTags.Where(t => t.SiteId == site.Id).Select(t => t.Tag.Name).ToListAsync(),
            Comments = comments,
        });
    }

    [HttpPost, Authorize, EnableRateLimiting("write")]
    public async Task<IActionResult> Follow(string name)
    {
        var site = await FindAsync(name);
        if (site is null) return NotFound();
        if (site.OwnerId == UserId) Flash("Kendi sitenizi takip edemezsiniz.", error: true);
        else await social.ToggleFollowAsync(UserId!, site.Id);
        return RedirectToAction(nameof(Profile), new { name = site.Name });
    }

    [HttpPost, Authorize, EnableRateLimiting("write")]
    public async Task<IActionResult> Like(string name)
    {
        var site = await FindAsync(name);
        if (site is null) return NotFound();
        if (site.OwnerId == UserId) Flash("Kendi sitenizi beğenemezsiniz.", error: true);
        else await social.ToggleLikeAsync(UserId!, site.Id);
        return RedirectToAction(nameof(Profile), new { name = site.Name });
    }

    [HttpPost, Authorize, EnableRateLimiting("write")]
    public async Task<IActionResult> Comment(string name, string body)
    {
        var site = await FindAsync(name);
        if (site is null || site.IsBanned) return NotFound();
        try { await social.AddCommentAsync(UserId!, site.Id, body); }
        catch (SiteFileException ex) { Flash(ex.Message, error: true); }
        return RedirectToAction(nameof(Profile), new { name = site.Name });
    }

    [HttpPost, Authorize]
    public async Task<IActionResult> DeleteComment(string name, int id)
    {
        if (!await social.DeleteCommentAsync(UserId!, id, User.IsInRole(DbInitializer.AdminRole)))
            Flash("Bu yorumu silemezsiniz.", error: true);
        return RedirectToAction(nameof(Profile), new { name });
    }

    [HttpPost, EnableRateLimiting("auth")]
    public async Task<IActionResult> Report(string name, ReportInputModel input)
    {
        var site = await FindAsync(name);
        if (site is null) return NotFound();
        db.Reports.Add(new Report
        {
            SiteId = site.Id,
            ReporterId = UserId,
            Reason = input.Reason,
            Details = input.Details?.Trim(),
        });
        await db.SaveChangesAsync();
        Flash("Şikayetiniz alındı, teşekkürler. Ekibimiz inceleyecek.");
        return RedirectToAction(nameof(Profile), new { name = site.Name });
    }
}
