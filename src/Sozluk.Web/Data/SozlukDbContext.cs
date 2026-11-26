using Microsoft.EntityFrameworkCore;
using Sozluk.Web.Models;

namespace Sozluk.Web.Data;

public class SozlukDbContext(DbContextOptions<SozlukDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<Entry> Entries => Set<Entry>();
    public DbSet<Vote> Votes => Set<Vote>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Report> Reports => Set<Report>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.HasIndex(x => x.UserName).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
        });

        b.Entity<Topic>(e =>
        {
            e.HasIndex(x => x.Title).IsUnique();
            e.HasIndex(x => x.LastEntryAt);
            e.HasOne(x => x.CreatedBy).WithMany()
                .HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Entry>(e =>
        {
            e.HasIndex(x => new { x.TopicId, x.CreatedAt });
            e.HasIndex(x => new { x.AuthorId, x.CreatedAt });
            e.HasIndex(x => x.CreatedAt);
            e.HasOne(x => x.Topic).WithMany(t => t.Entries)
                .HasForeignKey(x => x.TopicId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Author).WithMany(u => u.Entries)
                .HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Vote>(e =>
        {
            e.HasKey(x => new { x.EntryId, x.UserId });
            e.HasOne(x => x.Entry).WithMany(x => x.Votes)
                .HasForeignKey(x => x.EntryId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany()
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Favorite>(e =>
        {
            e.HasKey(x => new { x.EntryId, x.UserId });
            e.HasOne(x => x.Entry).WithMany()
                .HasForeignKey(x => x.EntryId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany()
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Message>(e =>
        {
            e.HasIndex(x => new { x.ReceiverId, x.IsRead });
            e.HasOne(x => x.Sender).WithMany()
                .HasForeignKey(x => x.SenderId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Receiver).WithMany()
                .HasForeignKey(x => x.ReceiverId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Report>(e =>
        {
            e.HasIndex(x => x.IsResolved);
            e.HasOne(x => x.Entry).WithMany()
                .HasForeignKey(x => x.EntryId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Reporter).WithMany()
                .HasForeignKey(x => x.ReporterId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
