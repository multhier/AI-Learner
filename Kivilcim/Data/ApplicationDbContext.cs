using Kivilcim.Models;
using Kivilcim.Services;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Kivilcim.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<PostTag> PostTags => Set<PostTag>();
    public DbSet<Like> Likes => Set<Like>();
    public DbSet<Follow> Follows => Set<Follow>();
    public DbSet<Reply> Replies => Set<Reply>();
    public DbSet<HiddenTag> HiddenTags => Set<HiddenTag>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<PostImage> PostImages => Set<PostImage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(e =>
        {
            e.Property(u => u.BlogName).HasMaxLength(32);
            e.HasIndex(u => u.BlogName).IsUnique();
            e.Property(u => u.DisplayName).HasMaxLength(64);
            e.Property(u => u.Bio).HasMaxLength(500);
            e.Property(u => u.AvatarPath).HasMaxLength(300);
        });

        // SQL Server aynı tabloya birden fazla cascade yolu kabul etmez.
        // Kullanıcı silinince gönderileri cascade ile silinir; beğeni, yanıt ve takipler
        // SaveChangesAsync içinde elle temizlenir (RemoveUserActivityAsync).
        builder.Entity<Post>(e =>
        {
            e.Property(p => p.Title).HasMaxLength(200);
            e.Property(p => p.VideoPath).HasMaxLength(300);
            e.Property(p => p.LinkUrl).HasMaxLength(2000);
            e.HasIndex(p => p.CreatedAt);
            e.HasOne(p => p.Author).WithMany(u => u.Posts)
                .HasForeignKey(p => p.AuthorId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(p => p.ReblogOf).WithMany(p => p.Reblogs)
                .HasForeignKey(p => p.ReblogOfId).OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<Tag>(e =>
        {
            e.Property(t => t.Name).HasMaxLength(TagParser.MaxTagLength);
            e.HasIndex(t => t.Name).IsUnique();
        });

        builder.Entity<PostTag>(e =>
        {
            e.HasKey(pt => new { pt.PostId, pt.TagId });
            e.HasOne(pt => pt.Post).WithMany(p => p.PostTags).HasForeignKey(pt => pt.PostId);
            e.HasOne(pt => pt.Tag).WithMany(t => t.PostTags).HasForeignKey(pt => pt.TagId);
        });

        builder.Entity<Like>(e =>
        {
            e.HasKey(l => new { l.UserId, l.PostId });
            e.HasOne(l => l.Post).WithMany(p => p.Likes)
                .HasForeignKey(l => l.PostId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(l => l.User).WithMany()
                .HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<Follow>(e =>
        {
            e.HasKey(f => new { f.FollowerId, f.FolloweeId });
            e.HasOne(f => f.Follower).WithMany(u => u.Following)
                .HasForeignKey(f => f.FollowerId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(f => f.Followee).WithMany(u => u.Followers)
                .HasForeignKey(f => f.FolloweeId).OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<Reply>(e =>
        {
            e.Property(r => r.Body).HasMaxLength(2000);
            e.HasOne(r => r.Post).WithMany(p => p.Replies)
                .HasForeignKey(r => r.PostId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.User).WithMany()
                .HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<HiddenTag>(e =>
        {
            e.HasKey(h => new { h.UserId, h.TagName });
            e.Property(h => h.TagName).HasMaxLength(TagParser.MaxTagLength);
            e.HasOne(h => h.User).WithMany(u => u.HiddenTags)
                .HasForeignKey(h => h.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        ConfigureSocial(builder);

        builder.Entity<PostImage>(e =>
        {
            e.Property(i => i.Url).HasMaxLength(2000);
            e.HasIndex(i => new { i.PostId, i.SortOrder });
            e.HasOne(i => i.Post).WithMany(p => p.Images)
                .HasForeignKey(i => i.PostId).OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureSocial(ModelBuilder builder)
    {
        // Bildirim: alıcı silinince cascade; aktör ve gönderi elle temizlenir (çoklu cascade yolu olmasın diye).
        builder.Entity<Notification>(e =>
        {
            e.HasIndex(n => new { n.RecipientId, n.IsRead, n.Id });
            e.HasOne(n => n.Recipient).WithMany()
                .HasForeignKey(n => n.RecipientId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(n => n.Actor).WithMany()
                .HasForeignKey(n => n.ActorId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(n => n.Post).WithMany()
                .HasForeignKey(n => n.PostId).OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<Message>(e =>
        {
            e.Property(m => m.Body).HasMaxLength(4000);
            e.HasIndex(m => new { m.SenderId, m.RecipientId, m.Id });
            e.HasIndex(m => new { m.RecipientId, m.ReadAt });
            e.HasOne(m => m.Sender).WithMany()
                .HasForeignKey(m => m.SenderId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(m => m.Recipient).WithMany()
                .HasForeignKey(m => m.RecipientId).OnDelete(DeleteBehavior.NoAction);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<ApplicationUser>().ToList())
        {
            if (entry.State == EntityState.Added)
            {
                if (string.IsNullOrWhiteSpace(entry.Entity.BlogName))
                {
                    entry.Entity.BlogName = await GenerateUniqueBlogNameAsync(
                        entry.Entity.Email ?? entry.Entity.UserName ?? "blog", cancellationToken);
                }
                if (string.IsNullOrWhiteSpace(entry.Entity.DisplayName))
                {
                    entry.Entity.DisplayName = entry.Entity.BlogName!;
                }
            }
            else if (entry.State == EntityState.Deleted)
            {
                await RemoveUserActivityAsync(entry.Entity.Id, cancellationToken);
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> GenerateUniqueBlogNameAsync(string source, CancellationToken ct)
    {
        var baseName = BlogNames.Slugify(source.Split('@')[0]);
        for (var i = 0; ; i++)
        {
            var candidate = i == 0 ? baseName : $"{baseName}-{i}";
            if (!await Users.AnyAsync(u => u.BlogName == candidate, ct))
            {
                return candidate;
            }
        }
    }

    private async Task RemoveUserActivityAsync(string userId, CancellationToken ct)
    {
        Likes.RemoveRange(await Likes.Where(l => l.UserId == userId).ToListAsync(ct));
        Replies.RemoveRange(await Replies.Where(r => r.UserId == userId).ToListAsync(ct));
        Follows.RemoveRange(await Follows
            .Where(f => f.FollowerId == userId || f.FolloweeId == userId).ToListAsync(ct));
        Messages.RemoveRange(await Messages
            .Where(m => m.SenderId == userId || m.RecipientId == userId).ToListAsync(ct));
        // Kullanıcının yaptığı ya da onun gönderilerine bağlı bildirimler (alıcı olanlar cascade ile gider).
        Notifications.RemoveRange(await Notifications
            .Where(n => n.ActorId == userId || (n.Post != null && n.Post.AuthorId == userId)).ToListAsync(ct));

        // Başkalarının bu kullanıcının gönderilerinden yaptığı reblog'lar kalır, asıl gönderi bağı kopar.
        var orphanedReblogs = await Posts
            .Where(p => p.AuthorId != userId && p.ReblogOf != null && p.ReblogOf.AuthorId == userId)
            .ToListAsync(ct);
        foreach (var reblog in orphanedReblogs)
        {
            reblog.ReblogOfId = null;
        }
    }
}
