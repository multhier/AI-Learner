using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebKoy.Web.Data;
using WebKoy.Web.Models;
using WebKoy.Web.Services;

namespace WebKoy.Web.Controllers;

[Authorize, Route("dashboard/[action]"), EnableRateLimiting("write")]
public class DashboardController(AppDbContext db, SiteFileService files, SocialService social, IOptions<PlatformOptions> options) : AppController
{
    private readonly PlatformOptions _opt = options.Value;

    private Task<Site?> MySiteAsync() => db.Sites.FirstOrDefaultAsync(s => s.OwnerId == UserId);

    [HttpGet("/dashboard")]
    public async Task<IActionResult> Index(string? dir = "")
    {
        var site = await MySiteAsync();
        if (site is null) return NotFound();
        var path = SitePath.Normalize(dir) ?? "";
        if (path.Length > 0 && await files.FindAsync(site.Id, path) is not { IsDirectory: true })
            return RedirectToAction(nameof(Index));

        return View(new DashboardViewModel
        {
            Site = site,
            Dir = path,
            Items = await files.ListAsync(site.Id, path),
            QuotaBytes = _opt.QuotaBytes,
            MaxFileBytes = _opt.MaxFileBytes,
            SiteUrl = _opt.SiteUrl(site.Name),
        });
    }

    [HttpPost]
    [RequestSizeLimit(1024L * 1024 * 1024)]
    public async Task<IActionResult> Upload(string? dir)
    {
        var site = await MySiteAsync();
        if (site is null) return NotFound();
        var uploads = Request.Form.Files;
        await Run(async () =>
        {
            var items = uploads.Select(f => new UploadItem(f.FileName, f.Length, f.OpenReadStream)).ToList();
            var n = await files.UploadAsync(site, dir ?? "", items, HttpContext.RequestAborted);
            Flash($"{n} dosya yüklendi.");
        });
        return RedirectToAction(nameof(Index), new { dir });
    }

    [HttpPost]
    public async Task<IActionResult> NewFolder(string? dir, string name)
    {
        var site = await MySiteAsync();
        if (site is null) return NotFound();
        await Run(async () =>
        {
            await files.CreateFolderAsync(site, SitePath.Combine(dir ?? "", name ?? ""));
            Flash("Klasör oluşturuldu.");
        });
        return RedirectToAction(nameof(Index), new { dir });
    }

    [HttpPost]
    public async Task<IActionResult> NewFile(string? dir, string name)
    {
        var site = await MySiteAsync();
        if (site is null) return NotFound();
        var path = SitePath.Combine(dir ?? "", name ?? "");
        var ok = await Run(() => files.SaveTextAsync(site, path, "", mustBeNew: true));
        return ok ? RedirectToAction(nameof(Edit), new { path = SitePath.Normalize(path) }) : RedirectToAction(nameof(Index), new { dir });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(string path)
    {
        var site = await MySiteAsync();
        if (site is null) return NotFound();
        await Run(async () =>
        {
            await files.DeleteAsync(site, path);
            Flash($"'{SitePath.FileName(path)}' silindi.");
        });
        return RedirectToAction(nameof(Index), new { dir = SitePath.Parent(SitePath.Normalize(path) ?? "") });
    }

    [HttpPost]
    public async Task<IActionResult> Rename(string path, string newPath)
    {
        var site = await MySiteAsync();
        if (site is null) return NotFound();
        await Run(async () =>
        {
            await files.RenameAsync(site, path, newPath);
            Flash("Yeniden adlandırıldı.");
        });
        return RedirectToAction(nameof(Index), new { dir = SitePath.Parent(SitePath.Normalize(path) ?? "") });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string path)
    {
        var site = await MySiteAsync();
        if (site is null) return NotFound();
        try
        {
            var p = SiteFileService.RequirePath(path);
            return View(new EditFileViewModel
            {
                Path = p,
                Content = await files.ReadTextAsync(site.Id, p),
                SiteUrl = _opt.SiteUrl(site.Name, p),
                Language = SitePath.Extension(p),
            });
        }
        catch (SiteFileException ex)
        {
            Flash(ex.Message, error: true);
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>Düzenleyici bu uç noktayı fetch ile çağırır (Ctrl+S); JSON döner.</summary>
    [HttpPost]
    public async Task<IActionResult> Save(string path, string? content)
    {
        var site = await MySiteAsync();
        if (site is null) return NotFound();
        try
        {
            await files.SaveTextAsync(site, path, content ?? "");
            return Json(new { ok = true, storage = Format.Bytes(site.StorageBytes) });
        }
        catch (SiteFileException ex)
        {
            return BadRequest(new { ok = false, error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Settings()
    {
        var site = await db.Sites.Include(s => s.Tags).ThenInclude(t => t.Tag).FirstOrDefaultAsync(s => s.OwnerId == UserId);
        if (site is null) return NotFound();
        return View(new SiteSettingsViewModel
        {
            Name = site.Name,
            Title = site.Title,
            Description = site.Description,
            Tags = string.Join(", ", site.Tags.Select(t => t.Tag.Name)),
        });
    }

    [HttpPost]
    public async Task<IActionResult> Settings(SiteSettingsViewModel model)
    {
        var site = await MySiteAsync();
        if (site is null) return NotFound();
        model.Name = site.Name;
        if (!ModelState.IsValid) return View(model);

        site.Title = string.IsNullOrWhiteSpace(model.Title) ? null : model.Title.Trim();
        site.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        await db.SaveChangesAsync();
        await social.SetTagsAsync(site, SocialService.ParseTags(model.Tags));
        Flash("Ayarlar kaydedildi.");
        return RedirectToAction(nameof(Settings));
    }

    private async Task<bool> Run(Func<Task> action)
    {
        try
        {
            await action();
            return true;
        }
        catch (SiteFileException ex)
        {
            Flash(ex.Message, error: true);
            return false;
        }
    }
}
