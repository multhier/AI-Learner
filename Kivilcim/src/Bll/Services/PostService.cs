using Kivilcim.Bll.Abstractions;
using Kivilcim.Bll.Dtos;
using Kivilcim.Bll.Media;
using Kivilcim.Bll.Rules;
using Kivilcim.Bll.Utilities;
using Kivilcim.Core.Abstractions;
using Kivilcim.Core.Realtime;
using Kivilcim.Core.Results;
using Kivilcim.Model;

namespace Kivilcim.Bll.Services;

/// <summary>Gönderi yazma işlemleri. Kurallar burada, veri erişimi Unit of Work üzerinden.</summary>
public sealed class PostService : IPostService
{
    private readonly IUnitOfWork _uow;
    private readonly IQueryExecutor _query;
    private readonly IFileStorage _files;
    private readonly INotificationService _notifications;
    private readonly IRealtimeNotifier _realtime;

    public PostService(IUnitOfWork uow, IQueryExecutor query, IFileStorage files,
        INotificationService notifications, IRealtimeNotifier realtime)
    {
        _uow = uow;
        _query = query;
        _files = files;
        _notifications = notifications;
        _realtime = realtime;
    }

    private IRepository<Post> Posts => _uow.Repository<Post>();

    public async Task<Result<int>> CreateAsync(string me, CreatePostCommand command, CancellationToken ct = default)
    {
        var title = Clean(command.Title);
        var body = Clean(command.Body);
        var link = Clean(command.LinkUrl);
        var files = command.Images.Where(f => f.Length > 0).ToList();
        var urls = command.ImageUrls.Select(u => u.Trim()).Where(u => u.Length > 0).Distinct().ToList();

        var errors = new List<ValidationError>();
        switch (command.Type)
        {
            case PostType.Text when title == null && body == null:
                errors.Add(new("Body", "Bir başlık ya da metin yazın."));
                break;
            case PostType.Quote when body == null:
                errors.Add(new("Body", "Alıntı metni zorunlu."));
                break;
            case PostType.Link when !UrlValidator.IsSafeHttpUrl(link):
                errors.Add(new("LinkUrl", "Geçerli bir http(s) bağlantısı girin."));
                break;
            case PostType.Photo when files.Count + urls.Count == 0:
                errors.Add(new("Images", "En az bir resim ekle."));
                break;
            case PostType.Photo when files.Count + urls.Count > PostRules.MaxImagesPerPost:
                errors.Add(new("Images", $"En fazla {PostRules.MaxImagesPerPost} resim ekleyebilirsin."));
                break;
            case PostType.Video when command.Video == null:
                errors.Add(new("Video", "Bir video seçin."));
                break;
        }

        foreach (var u in urls.Where(u => u.Length > 2000 || !UrlValidator.IsSafeHttpUrl(u)))
        {
            errors.Add(new("ImageUrls", $"Geçersiz resim bağlantısı: {(u.Length > 60 ? u[..60] + "…" : u)}"));
        }
        foreach (var f in files.Where(_ => command.Type == PostType.Photo))
        {
            var error = MediaValidator.ValidateImage(f);
            if (error != null) errors.Add(new("Images", $"{f.FileName}: {error}"));
        }
        if (command.Type == PostType.Video && command.Video != null)
        {
            var error = MediaValidator.ValidateVideo(command.Video);
            if (error != null) errors.Add(new("Video", error));
        }
        if (errors.Count > 0)
        {
            return Result.Failure<int>(errors);
        }

        var saved = new List<string>();
        try
        {
            var images = new List<PostImage>();
            if (command.Type == PostType.Photo)
            {
                foreach (var f in files)
                {
                    await using var stream = f.OpenRead();
                    var path = await _files.SaveAsync(stream, "posts", MediaValidator.Extension(f.FileName), ct);
                    saved.Add(path);
                    images.Add(new PostImage { Url = path, SortOrder = images.Count });
                }
                foreach (var u in urls)
                {
                    images.Add(new PostImage { Url = u, IsExternal = true, SortOrder = images.Count });
                }
            }

            string? videoPath = null;
            if (command.Type == PostType.Video)
            {
                var video = command.Video!;
                await using var stream = video.OpenRead();
                videoPath = await _files.SaveAsync(stream, "videos", MediaValidator.Extension(video.FileName), ct);
                saved.Add(videoPath);
            }

            var post = new Post
            {
                AuthorId = me,
                Type = command.Type,
                Title = title,
                Body = body,
                LinkUrl = command.Type == PostType.Link ? link : null,
                VideoPath = videoPath,
                Images = images
            };
            await AttachTagsAsync(post, command.Tags, ct);
            await Posts.AddAsync(post, ct);
            await _uow.SaveChangesAsync(ct);
            return Result.Success(post.Id);
        }
        catch
        {
            foreach (var path in saved)
            {
                _files.Delete(path);
            }
            throw;
        }
    }

    public async Task<Result> UpdateAsync(string me, int postId, EditPostCommand command, CancellationToken ct = default)
    {
        var post = await Posts.GetByIdAsync(postId);
        if (post == null)
        {
            return Result.Failure(new[] { new ValidationError("NotFound", "Gönderi bulunamadı.") });
        }
        if (post.AuthorId != me)
        {
            return Result.Failure(new[] { new ValidationError("Forbidden", "Bu gönderiyi düzenleme yetkin yok.") });
        }

        var title = Clean(command.Title);
        var body = Clean(command.Body);
        var link = Clean(command.LinkUrl);
        var errors = new List<ValidationError>();

        switch (post.Type)
        {
            case PostType.Text when title == null && body == null:
                errors.Add(new("Body", "Bir başlık ya da metin yazın."));
                break;
            case PostType.Quote when body == null:
                errors.Add(new("Body", "Alıntı metni zorunlu."));
                break;
            case PostType.Link when !UrlValidator.IsSafeHttpUrl(link):
                errors.Add(new("LinkUrl", "Geçerli bir http(s) bağlantısı girin."));
                break;
        }
        if (errors.Count > 0)
        {
            return Result.Failure(errors);
        }

        post.Title = title;
        post.Body = body;
        if (post.Type == PostType.Link)
        {
            post.LinkUrl = link;
        }

        await ReplaceTagsAsync(post, command.Tags, ct);
        await _uow.SaveChangesAsync(ct);
        await _realtime.SendToGroupAsync(RealtimeGroups.Post(postId), "postUpdated", new { id = postId }, ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(string me, int postId, CancellationToken ct = default)
    {
        var post = await Posts.GetByIdAsync(postId);
        if (post == null)
        {
            return Result.Failure(new[] { new ValidationError("NotFound", "Gönderi bulunamadı.") });
        }
        if (post.AuthorId != me)
        {
            return Result.Failure(new[] { new ValidationError("Forbidden", "Başkasının gönderisini silemezsin.") });
        }

        var localFiles = await _query.ToListAsync(_uow.Repository<PostImage>().Query()
            .Where(i => i.PostId == postId && !i.IsExternal).Select(i => i.Url), ct);
        if (post.VideoPath != null)
        {
            localFiles.Add(post.VideoPath);
        }

        // Bu gönderiye bağlı bildirimler ve reblog'lar: reblog'lar kalır, yalnızca asıl bağlantı kopar.
        var reblogs = await _query.ToListAsync(Posts.Query().Where(p => p.ReblogOfId == postId), ct);
        foreach (var r in reblogs)
        {
            r.ReblogOfId = null;
        }
        var notes = await _query.ToListAsync(_uow.Repository<Notification>().Query().Where(n => n.PostId == postId), ct);
        _uow.Repository<Notification>().RemoveRange(notes);

        Posts.Remove(post);
        await _uow.SaveChangesAsync(ct);

        // Dosyalar veritabanı kaydı silindikten sonra silinir; böylece yarım kalan bir silme dosyayı kaybettirmez.
        foreach (var path in localFiles)
        {
            _files.Delete(path);
        }
        return Result.Success();
    }

    public async Task<LikeResult?> ToggleLikeAsync(string me, int postId, CancellationToken ct = default)
    {
        var authorId = await _query.FirstOrDefaultAsync(Posts.Query().Where(p => p.Id == postId).Select(p => p.AuthorId), ct);
        if (authorId == null)
        {
            return null;
        }

        var existing = await _uow.Repository<Like>().GetByIdAsync(me, postId);
        if (existing == null)
        {
            await _uow.Repository<Like>().AddAsync(new Like { UserId = me, PostId = postId }, ct);
        }
        else
        {
            _uow.Repository<Like>().Remove(existing);
        }
        await _uow.SaveChangesAsync(ct);

        if (existing == null)
        {
            await _notifications.NotifyAsync(authorId, me, NotificationType.Like, postId, ct);
        }
        else
        {
            await _notifications.RetractAsync(authorId, me, NotificationType.Like, postId, ct);
        }

        var count = await _query.CountAsync(_uow.Repository<Like>().Query().Where(l => l.PostId == postId), ct);
        return new LikeResult(existing == null, count);
    }

    public async Task<Result<int>> ReblogAsync(string me, int sourceId, string? comment, string? tags, CancellationToken ct = default)
    {
        var source = await _query.FirstOrDefaultAsync(Posts.Query()
            .Where(p => p.Id == sourceId)
            .Select(p => new { p.Type, p.ReblogOfId, RootAuthorId = p.ReblogOf != null ? p.ReblogOf.AuthorId : p.AuthorId }), ct);
        if (source == null)
        {
            return Result.Failure<int>("Gönderi bulunamadı.");
        }

        // Her zaman asıl gönderiye bağlanır; reblog zinciri uzamaz.
        var rootId = source.ReblogOfId ?? sourceId;
        var text = Clean(comment);
        if (text != null && text.Length > PostRules.MaxBodyLength)
        {
            text = text[..PostRules.MaxBodyLength];
        }

        var reblog = new Post { AuthorId = me, Type = source.Type, ReblogOfId = rootId, Body = text };
        await AttachTagsAsync(reblog, tags, ct);
        await Posts.AddAsync(reblog, ct);
        await _uow.SaveChangesAsync(ct);

        await _notifications.NotifyAsync(source.RootAuthorId, me, NotificationType.Reblog, rootId, ct);
        return Result.Success(reblog.Id);
    }

    public async Task<Result<ReplyViewModel>> ReplyAsync(string me, int postId, string? body, CancellationToken ct = default)
    {
        var authorId = await _query.FirstOrDefaultAsync(Posts.Query().Where(p => p.Id == postId).Select(p => p.AuthorId), ct);
        if (authorId == null)
        {
            return Result.Failure<ReplyViewModel>("Gönderi bulunamadı.");
        }

        var text = Clean(body);
        if (text == null)
        {
            return Result.Failure<ReplyViewModel>("Yanıt boş olamaz.");
        }
        if (text.Length > PostRules.MaxReplyLength)
        {
            text = text[..PostRules.MaxReplyLength];
        }

        var reply = new Reply { PostId = postId, UserId = me, Body = text };
        await _uow.Repository<Reply>().AddAsync(reply, ct);
        await _uow.SaveChangesAsync(ct);

        await _notifications.NotifyAsync(authorId, me, NotificationType.Reply, postId, ct);

        var view = await _query.FirstOrDefaultAsync(_uow.Repository<Reply>().Query()
            .Where(r => r.Id == reply.Id)
            .Select(r => new ReplyViewModel(r.Id, r.User.BlogName!, r.User.DisplayName, r.User.AvatarPath, r.Body, r.CreatedAt)), ct);
        if (view == null)
        {
            return Result.Failure<ReplyViewModel>("Yanıt kaydedilemedi.");
        }

        // Gönderiyi açık tutan herkesin sayfasına canlı olarak düşer.
        await _realtime.SendToGroupAsync(RealtimeGroups.Post(postId), "reply", view, ct);
        return Result.Success(view);
    }

    private async Task AttachTagsAsync(Post post, string? rawTags, CancellationToken ct)
    {
        var names = TagParser.Parse(rawTags);
        if (names.Count == 0)
        {
            return;
        }

        var existing = await _query.ToListAsync(_uow.Repository<Tag>().Query().Where(t => names.Contains(t.Name)), ct);
        foreach (var name in names)
        {
            var tag = existing.FirstOrDefault(t => t.Name == name) ?? new Tag { Name = name };
            post.PostTags.Add(new PostTag { Tag = tag });
        }
    }

    /// <summary>Düzenlemede etiketleri farka göre günceller: aynı (gönderi, etiket) çifti iki kez izlenmez.</summary>
    private async Task ReplaceTagsAsync(Post post, string? rawTags, CancellationToken ct)
    {
        var wanted = TagParser.Parse(rawTags).ToHashSet();
        // Etiket adı projeksiyonla alınır; navigasyon yüklenmeden de doğru çalışır.
        var current = await _query.ToListAsync(_uow.Repository<PostTag>().Query()
            .Where(pt => pt.PostId == post.Id)
            .Select(pt => new { Link = pt, Name = pt.Tag.Name }), ct);

        var toRemove = current.Where(x => !wanted.Contains(x.Name)).Select(x => x.Link).ToList();
        _uow.Repository<PostTag>().RemoveRange(toRemove);

        var keep = current.Select(x => x.Name).ToHashSet();
        var missing = wanted.Where(n => !keep.Contains(n)).ToList();
        if (missing.Count == 0)
        {
            return;
        }

        var existingTags = await _query.ToListAsync(_uow.Repository<Tag>().Query().Where(t => missing.Contains(t.Name)), ct);
        foreach (var name in missing)
        {
            var tag = existingTags.FirstOrDefault(t => t.Name == name) ?? new Tag { Name = name };
            await _uow.Repository<PostTag>().AddAsync(new PostTag { PostId = post.Id, Tag = tag }, ct);
        }
    }

    private static string? Clean(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
