using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace WebKoy.Web.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Site> Sites => Set<Site>();
    public DbSet<SiteFile> SiteFiles => Set<SiteFile>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<SiteTag> SiteTags => Set<SiteTag>();
    public DbSet<Follow> Follows => Set<Follow>();
    public DbSet<Like> Likes => Set<Like>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Report> Reports => Set<Report>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<Site>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(32).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Title).HasMaxLength(100);
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.BanReason).HasMaxLength(500);
            e.HasIndex(x => x.UpdatedAt);
            e.HasIndex(x => x.Views);
            // Kullanıcı başına tek site.
            e.HasOne(x => x.Owner).WithOne(u => u.Site)
                .HasForeignKey<Site>(x => x.OwnerId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.OwnerId).IsUnique();
        });

        b.Entity<SiteFile>(e =>
        {
            e.Property(x => x.Path).HasMaxLength(400).IsRequired();
            e.HasIndex(x => new { x.SiteId, x.Path }).IsUnique();
            e.HasOne(x => x.Site).WithMany(s => s.Files).HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Tag>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(25).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
        });

        b.Entity<SiteTag>(e =>
        {
            e.HasKey(x => new { x.SiteId, x.TagId });
            e.HasOne(x => x.Site).WithMany(s => s.Tags).HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Tag).WithMany(t => t.Sites).HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Cascade);
        });

        // Kullanıcı -> Site ilişkilerinde SQL Server çoklu cascade yolu hatasını önlemek için
        // kullanıcı tarafı NoAction; kullanıcı silme işlemi AccountService içinde temizlenir.
        b.Entity<Follow>(e =>
        {
            e.HasKey(x => new { x.UserId, x.SiteId });
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Site).WithMany(s => s.Followers).HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Like>(e =>
        {
            e.HasKey(x => new { x.UserId, x.SiteId });
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Site).WithMany(s => s.Likes).HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Comment>(e =>
        {
            e.Property(x => x.Body).HasMaxLength(1000).IsRequired();
            e.HasIndex(x => new { x.SiteId, x.CreatedAt });
            e.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Site).WithMany(s => s.Comments).HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Report>(e =>
        {
            e.Property(x => x.Details).HasMaxLength(2000);
            e.Property(x => x.Reason).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => new { x.Resolved, x.CreatedAt });
            e.HasOne(x => x.Reporter).WithMany().HasForeignKey(x => x.ReporterId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Site).WithMany().HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
