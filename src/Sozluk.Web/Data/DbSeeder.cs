using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sozluk.Web.Models;
using Sozluk.Web.Services;

namespace Sozluk.Web.Data;

public static class DbSeeder
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration config)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SozlukDbContext>();

        if (db.Database.IsSqlServer())
            await db.Database.MigrateAsync();
        else
            await db.Database.EnsureCreatedAsync();

        if (await db.Users.AnyAsync()) return;

        var hasher = new PasswordHasher<User>();
        var adminPassword = config["Seed:AdminPassword"] ?? "Admin123!";

        var admin = new User { UserName = "admin", Email = "admin@example.com", Role = UserRole.Admin };
        admin.PasswordHash = hasher.HashPassword(admin, adminPassword);
        var yazar = new User { UserName = "ilk yazar", Email = "yazar@example.com", Role = UserRole.Yazar };
        yazar.PasswordHash = hasher.HashPassword(yazar, "Yazar123!");
        db.Users.AddRange(admin, yazar);
        await db.SaveChangesAsync();

        var samples = new (string title, string[] entries)[]
        {
            ("sözlük", new[] {
                "herkesin her konuda yazabildiği, başlıklar ve entry'lerden oluşan ortak metin.",
                "kurallarını öğrenmeden yazılmaz. (bkz: sözlük kuralları)" }),
            ("sözlük kuralları", new[] {
                "başlıklar küçük harfle açılır, entry'ler başlığın tanımı ya da başlıkla ilgili görüş olmalıdır.\n(bkz: `bkz`)" }),
            ("bkz", new[] { "bakınız'ın kısaltması. başka bir başlığa yönlendirmek için kullanılır." }),
            ("çaylak", new[] { "yeni kayıt olmuş, entry'leri moderasyon onayından sonra yazarlığa terfi edecek kullanıcı." }),
        };

        var now = DateTime.UtcNow;
        foreach (var (title, entries) in samples)
        {
            var topic = new Topic
            {
                Title = title, Slug = Slug.Create(title), CreatedById = yazar.Id,
                CreatedAt = now, LastEntryAt = now, EntryCount = entries.Length
            };
            foreach (var e in entries)
                topic.Entries.Add(new Entry { AuthorId = yazar.Id, Content = e, CreatedAt = now });
            db.Topics.Add(topic);
        }
        await db.SaveChangesAsync();
    }
}
