using System.Security.Claims;
using Kivilcim.Data;
using Kivilcim.Models;
using Kivilcim.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kivilcim.Controllers;

public class PostsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly PostQueryService _posts;
    private readonly ImageStore _images;
    private readonly NotificationService _notifications;

    public PostsController(ApplicationDbContext db, PostQueryService posts, ImageStore images, NotificationService notifications)
    {
        _db = db;
        _posts = posts;
        _images = images;
        _notifications = notifications;
    }

    private string? Me => User.FindFirstValue(ClaimTypes.NameIdentifier);

    [HttpGet("/post/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var post = await _posts.GetOneAsync(id, Me);
        if (post == null) return NotFound();

        var replies = await _db.Replies
            .Where(r => r.PostId == id)
            .OrderBy(r => r.Id)
            .Select(r => new ReplyViewModel(r.User.BlogName!, r.User.DisplayName, r.User.AvatarPath, r.Body, r.CreatedAt))
            .ToListAsync();

        var likes = await _db.Likes
            .Where(l => l.PostId == id)
            .OrderByDescending(l => l.CreatedAt).Take(20)
            .Select(l => new NoteViewModel("like", l.User.BlogName!, l.CreatedAt))
            .ToListAsync();
        var reblogs = await _db.Posts
            .Where(p => p.ReblogOfId == id)
            .OrderByDescending(p => p.Id).Take(20)
            .Select(p => new NoteViewModel("reblog", p.Author.BlogName!, p.CreatedAt))
            .ToListAsync();

        return View(new PostDetailsViewModel
        {
            Post = post,
            Replies = replies,
            RecentNotes = likes.Concat(reblogs).OrderByDescending(n => n.CreatedAt).Take(20).ToList()
        });
    }

    [Authorize]
    [HttpGet("/new/{type?}")]
    public IActionResult Create(PostType type = PostType.Text)
    {
        return View(new CreatePostViewModel { Type = type });
    }

    [Authorize]
    [HttpPost("/new/{type?}")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(55 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 55 * 1024 * 1024)]
    public async Task<IActionResult> Create(CreatePostViewModel model)
    {
        var title = model.Title?.Trim();
        var body = model.Body?.Trim();
        var link = model.LinkUrl?.Trim();

        switch (model.Type)
        {
            case PostType.Text when string.IsNullOrEmpty(title) && string.IsNullOrEmpty(body):
                ModelState.AddModelError(nameof(model.Body), "Bir başlık ya da metin yazın.");
                break;
            case PostType.Quote when string.IsNullOrEmpty(body):
                ModelState.AddModelError(nameof(model.Body), "Alıntı metni zorunlu.");
                break;
            case PostType.Link when !TextFormat.IsSafeHttpUrl(link):
                ModelState.AddModelError(nameof(model.LinkUrl), "Geçerli bir http(s) bağlantısı girin.");
                break;
            case PostType.Photo when model.Image == null:
                ModelState.AddModelError(nameof(model.Image), "Bir görsel seçin.");
                break;
            case PostType.Video when model.Video == null:
                ModelState.AddModelError(nameof(model.Video), "Bir video seçin.");
                break;
        }

        string? imagePath = null;
        if (ModelState.IsValid && model.Type == PostType.Photo && model.Image != null)
        {
            var (path, error) = await _images.SaveAsync(model.Image, "posts");
            if (error != null) ModelState.AddModelError(nameof(model.Image), error);
            imagePath = path;
        }

        string? videoPath = null;
        if (ModelState.IsValid && model.Type == PostType.Video && model.Video != null)
        {
            var (path, error) = await _images.SaveVideoAsync(model.Video);
            if (error != null) ModelState.AddModelError(nameof(model.Video), error);
            videoPath = path;
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var post = new Post
        {
            AuthorId = Me!,
            Type = model.Type,
            Title = string.IsNullOrEmpty(title) ? null : title,
            Body = string.IsNullOrEmpty(body) ? null : body,
            LinkUrl = model.Type == PostType.Link ? link : null,
            ImagePath = imagePath,
            VideoPath = videoPath
        };
        await AttachTagsAsync(post, model.Tags);
        _db.Posts.Add(post);
        await _db.SaveChangesAsync();

        TempData["Message"] = "Gönderin paylaşıldı.";
        return RedirectToAction(nameof(Details), new { id = post.Id });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Like(int id, string? returnUrl)
    {
        var me = Me!;
        var authorId = await _db.Posts.Where(p => p.Id == id).Select(p => p.AuthorId).FirstOrDefaultAsync();
        if (authorId == null) return NotFound();

        var existing = await _db.Likes.FindAsync(me, id);
        if (existing == null)
        {
            _db.Likes.Add(new Like { UserId = me, PostId = id });
        }
        else
        {
            _db.Likes.Remove(existing);
        }
        await _db.SaveChangesAsync();

        if (existing == null)
            await _notifications.NotifyAsync(authorId, me, NotificationType.Like, id);
        else
            await _notifications.RetractAsync(authorId, me, NotificationType.Like, id);

        if (IsFetch())
        {
            var count = await _db.Likes.CountAsync(l => l.PostId == id);
            return Json(new { liked = existing == null, count });
        }
        return BackTo(returnUrl, id);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reblog(int id, string? comment, string? tags, string? returnUrl)
    {
        var source = await _db.Posts.AsNoTracking()
            .Select(p => new { p.Id, p.ReblogOfId, p.Type, RootAuthorId = p.ReblogOf != null ? p.ReblogOf.AuthorId : p.AuthorId })
            .FirstOrDefaultAsync(p => p.Id == id);
        if (source == null) return NotFound();

        // Zincir yerine her zaman asıl gönderiye bağla.
        var rootId = source.ReblogOfId ?? source.Id;
        var text = comment?.Trim();
        if (text?.Length > 2000) text = text[..2000];

        var reblog = new Post
        {
            AuthorId = Me!,
            Type = source.Type,
            ReblogOfId = rootId,
            Body = string.IsNullOrEmpty(text) ? null : text
        };
        await AttachTagsAsync(reblog, tags);
        _db.Posts.Add(reblog);
        await _db.SaveChangesAsync();
        await _notifications.NotifyAsync(source.RootAuthorId, Me!, NotificationType.Reblog, rootId);

        TempData["Message"] = "Yeniden paylaşıldı.";
        return BackTo(returnUrl, reblog.Id);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(int id, string? body)
    {
        var authorId = await _db.Posts.Where(p => p.Id == id).Select(p => p.AuthorId).FirstOrDefaultAsync();
        if (authorId == null) return NotFound();

        var text = body?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            TempData["Error"] = "Yanıt boş olamaz.";
        }
        else
        {
            if (text.Length > 2000) text = text[..2000];
            _db.Replies.Add(new Reply { PostId = id, UserId = Me!, Body = text });
            await _db.SaveChangesAsync();
            await _notifications.NotifyAsync(authorId, Me!, NotificationType.Reply, id);
        }
        return Redirect(Url.Action(nameof(Details), new { id }) + "#yanitlar");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == id);
        if (post == null) return NotFound();
        if (post.AuthorId != Me) return Forbid();

        var reblogs = await _db.Posts.Where(p => p.ReblogOfId == id).ToListAsync();
        foreach (var r in reblogs) r.ReblogOfId = null;
        _db.Notifications.RemoveRange(await _db.Notifications.Where(n => n.PostId == id).ToListAsync());

        _db.Posts.Remove(post);
        await _db.SaveChangesAsync();
        _images.Delete(post.ImagePath);
        _images.Delete(post.VideoPath);

        TempData["Message"] = "Gönderi silindi.";
        var blog = await _db.Users.Where(u => u.Id == post.AuthorId).Select(u => u.BlogName).FirstAsync();
        return RedirectToAction("Index", "Blog", new { blogName = blog });
    }

    private async Task AttachTagsAsync(Post post, string? rawTags)
    {
        var names = TagParser.Parse(rawTags);
        if (names.Count == 0) return;

        var existing = await _db.Tags.Where(t => names.Contains(t.Name)).ToListAsync();
        foreach (var name in names)
        {
            var tag = existing.FirstOrDefault(t => t.Name == name) ?? new Tag { Name = name };
            post.PostTags.Add(new PostTag { Tag = tag });
        }
    }

    private bool IsFetch() => Request.Headers["X-Requested-With"] == "fetch";

    private IActionResult BackTo(string? returnUrl, int postId) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction(nameof(Details), new { id = postId });
}
