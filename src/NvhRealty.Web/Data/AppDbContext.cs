using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NvhRealty.Web.Models.Entities;

namespace NvhRealty.Web.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Area> Areas => Set<Area>();
    public DbSet<Neighborhood> Neighborhoods => Set<Neighborhood>();
    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<ListingPhoto> ListingPhotos => Set<ListingPhoto>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Testimonial> Testimonials => Set<Testimonial>();
    public DbSet<ContentPage> ContentPages => Set<ContentPage>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<CareerApplication> CareerApplications => Set<CareerApplication>();
    public DbSet<SearchAlert> SearchAlerts => Set<SearchAlert>();
    public DbSet<FavoriteListing> FavoriteListings => Set<FavoriteListing>();
    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<Area>().HasIndex(x => x.Name).IsUnique();
        b.Entity<Neighborhood>().HasIndex(x => new { x.AreaId, x.Name }).IsUnique();

        b.Entity<Listing>(e =>
        {
            e.HasIndex(x => new { x.ListingType, x.Status, x.Price });
            e.HasIndex(x => x.ListedOn);
            e.HasOne(x => x.Neighborhood).WithMany().HasForeignKey(x => x.NeighborhoodId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Agent).WithMany(a => a.Listings).HasForeignKey(x => x.AgentId).OnDelete(DeleteBehavior.SetNull);
            e.HasMany(x => x.Photos).WithOne(p => p.Listing!).HasForeignKey(p => p.ListingId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Post>().HasIndex(x => x.Slug).IsUnique();
        b.Entity<ContentPage>().HasIndex(x => x.Slug).IsUnique();

        b.Entity<Testimonial>().HasOne(x => x.Agent).WithMany(a => a.Testimonials)
            .HasForeignKey(x => x.AgentId).OnDelete(DeleteBehavior.SetNull);

        b.Entity<ContactMessage>(e =>
        {
            e.HasOne(x => x.Agent).WithMany().HasForeignKey(x => x.AgentId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Listing).WithMany().HasForeignKey(x => x.ListingId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<FavoriteListing>(e =>
        {
            e.HasKey(x => new { x.UserId, x.ListingId });
            e.HasOne(x => x.User).WithMany(u => u.Favorites).HasForeignKey(x => x.UserId);
            e.HasOne(x => x.Listing).WithMany().HasForeignKey(x => x.ListingId);
        });
    }
}
