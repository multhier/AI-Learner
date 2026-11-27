using Kivilcim.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kivilcim.Dal.Data;

/// <summary>Boş veritabanına örnek kullanıcı ve gönderiler ekler (yalnızca geliştirme için).</summary>
public static class SeedData
{
    public const string DemoPassword = "Demo1234";

    public static async Task EnsureSeededAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        if (await db.Users.AnyAsync())
        {
            return;
        }

        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        var people = new[]
        {
            ("ayse@ornek.com", "ayse", "Ayşe", "Fotoğraf, kahve ve uzun yürüyüşler."),
            ("mehmet@ornek.com", "mehmet-yazar", "Mehmet", "Kısa öyküler ve kitap notları."),
            ("zeynep@ornek.com", "zeynep-kodluyor", "Zeynep", "Yazılım, oyunlar ve kediler.")
        };

        var created = new List<ApplicationUser>();
        foreach (var (email, blog, name, bio) in people)
        {
            var user = new ApplicationUser
            {
                UserName = email, Email = email, EmailConfirmed = true,
                BlogName = blog, DisplayName = name, Bio = bio
            };
            var result = await users.CreateAsync(user, DemoPassword);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
            }
            created.Add(user);
        }

        var (ayse, mehmet, zeynep) = (created[0], created[1], created[2]);
        var tags = new Dictionary<string, Tag>();
        Tag T(string n) => tags.TryGetValue(n, out var t) ? t : tags[n] = new Tag { Name = n };
        Post P(ApplicationUser a, PostType type, string? title, string? body, int hoursAgo, params string[] tagNames)
        {
            var p = new Post { Author = a, Type = type, Title = title, Body = body, CreatedAt = DateTime.UtcNow.AddHours(-hoursAgo) };
            foreach (var n in tagNames) p.PostTags.Add(new PostTag { Tag = T(n) });
            return p;
        }

        var posts = new List<Post>
        {
            P(ayse, PostType.Text, "Sabah yürüyüşü", "Bugün sahilde 5 km yürüdüm.\nDeniz çok sakindi, martılar her yerdeydi.", 30, "yürüyüş", "deniz"),
            P(mehmet, PostType.Quote, null, "Okumak, başkasının aklıyla düşünmektir.", 26, "kitap", "alıntı"),
            P(zeynep, PostType.Text, "İlk ASP.NET Core projem", "Bugün MVC ile küçük bir blog sitesi yaptım. Entity Framework Core gerçekten kolaylaştırıyor.", 20, "yazılım", "aspnetcore"),
            P(ayse, PostType.Text, null, "Kahve olmadan güne başlayamayanlar burada mı? ☕", 12, "kahve"),
            P(zeynep, PostType.Link, "Bootstrap 5 belgeleri", "Arayüz için harika bir başlangıç noktası.", 8, "yazılım", "tasarım"),
            P(mehmet, PostType.Text, "Bir öykü denemesi", "Yağmur başladığında kasaba sessizleşti. Herkes pencerelere çekildi, bir tek yaşlı saatçi dükkânını açık bıraktı...", 4, "öykü", "kitap"),
            P(zeynep, PostType.Text, null, "Kedim klavyenin üstünde uyuyor, bugünlük kod yazmak yok. 🐈", 2, "kediler")
        };
        posts[4].LinkUrl = "https://getbootstrap.com/";
        db.Posts.AddRange(posts);

        db.Follows.AddRange(
            new Follow { Follower = ayse, Followee = zeynep },
            new Follow { Follower = ayse, Followee = mehmet },
            new Follow { Follower = zeynep, Followee = ayse },
            new Follow { Follower = mehmet, Followee = zeynep });

        db.Likes.AddRange(
            new Like { User = ayse, Post = posts[2] },
            new Like { User = mehmet, Post = posts[2] },
            new Like { User = zeynep, Post = posts[0] },
            new Like { User = ayse, Post = posts[6] },
            new Like { User = zeynep, Post = posts[1] });

        db.Replies.Add(new Reply { User = ayse, Post = posts[2], Body = "Tebrikler! Kodunu paylaşır mısın?" });

        db.Posts.Add(new Post
        {
            Author = ayse, Type = PostType.Text, ReblogOf = posts[1],
            Body = "Çok doğru.", CreatedAt = DateTime.UtcNow.AddHours(-1)
        });

        await db.SaveChangesAsync();
    }
}
