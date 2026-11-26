using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NvhRealty.Web.Models.Entities;
using NvhRealty.Web.Services;

namespace NvhRealty.Web.Data;

/// <summary>
/// Veritabanını oluşturur (migration) ve demo verilerini ekler.
/// Semt listesi orijinal sitenin arama filtresinden (REA ile yakalanan sayfa) çıkarıldı: Data/Seed/neighborhoods.json
/// Danışman, ilan, yazı ve yorumlar ise tamamen kurgusal demo içeriktir.
/// </summary>
public static class DbSeeder
{
    public const string AdminRole = "Admin";

    public static async Task SeedAsync(IServiceProvider sp, IConfiguration config, IWebHostEnvironment env)
    {
        var db = sp.GetRequiredService<AppDbContext>();
        if (db.Database.IsRelational()) await db.Database.MigrateAsync();
        else await db.Database.EnsureCreatedAsync();

        await SeedIdentityAsync(sp, config);

        if (!await db.SiteSettings.AnyAsync())
        {
            db.SiteSettings.AddRange(SiteSettingsService.Defaults.Select(kv => new SiteSetting { Key = kv.Key, Value = kv.Value }));
            await db.SaveChangesAsync();
        }

        if (!await db.Areas.AnyAsync())
        {
            var path = Path.Combine(env.ContentRootPath, "Data", "Seed", "neighborhoods.json");
            var map = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(await File.ReadAllTextAsync(path))!;
            var order = 0;
            foreach (var (area, hoods) in map)
            {
                db.Areas.Add(new Area
                {
                    Name = area,
                    SortOrder = order++,
                    Neighborhoods = hoods.Distinct().Select(h => new Neighborhood { Name = h }).ToList()
                });
            }
            await db.SaveChangesAsync();
        }

        if (!await db.Agents.AnyAsync())
        {
            db.Agents.AddRange(
                new Agent { FirstName = "Elena", LastName = "Marlow", Title = "Principal Broker / President", IsManagement = true, Email = "elena@example.com", Phone = "(212) 555-0100", Languages = "English, Russian", SortOrder = 0, PhotoUrl = "/img/agent-placeholder.svg", Bio = "<p>Kurucu ve sorumlu broker. 20 yılı aşkın satış, kiralama ve yeni proje pazarlama deneyimi.</p>" },
                new Agent { FirstName = "Daniel", LastName = "Reyes", Title = "Licensed Real Estate Salesperson", Email = "daniel@example.com", Phone = "(212) 555-0101", Languages = "English, Español", SortOrder = 1, PhotoUrl = "/img/agent-placeholder.svg", Bio = "<p>Upper West Side ve Harlem kiralamalarında uzman.</p>" },
                new Agent { FirstName = "Mia", LastName = "Chen", Title = "Licensed Real Estate Salesperson", Email = "mia@example.com", Phone = "(212) 555-0102", Languages = "English, Chinese", SortOrder = 2, IsFeatured = true, PhotoUrl = "/img/agent-placeholder.svg", Bio = "<p>Her müşterisine kişiye özel ilgi gösterir; ev sahipliği ve yatırımın finansal etkilerini anlaşılır kılar. Manhattan, Queens ve Brooklyn'de satış ve kiralama konusunda uzmandır.</p>" },
                new Agent { FirstName = "Oliver", LastName = "Stone", Title = "Licensed Associate Broker", Email = "oliver@example.com", Phone = "(212) 555-0103", Languages = "English, German, Italian", SortOrder = 3, PhotoUrl = "/img/agent-placeholder.svg", Bio = "<p>Ticari gayrimenkul ve yatırım mülkleri.</p>" },
                new Agent { FirstName = "Sofia", LastName = "Kowalski", Title = "Licensed Real Estate Salesperson", Email = "sofia@example.com", Phone = "(212) 555-0104", Languages = "English, Polish, Português", SortOrder = 4, PhotoUrl = "/img/agent-placeholder.svg", Bio = "<p>Brooklyn ve Florida bölgesinde ilk kez ev alanlara rehberlik eder.</p>" });
            await db.SaveChangesAsync();
        }

        if (!await db.Listings.AnyAsync())
        {
            db.Listings.AddRange(GenerateListings(await db.Neighborhoods.Include(n => n.Area).ToListAsync(), await db.Agents.ToListAsync()));
            await db.SaveChangesAsync();
        }

        if (!await db.Testimonials.AnyAsync())
        {
            var agents = await db.Agents.OrderBy(a => a.SortOrder).ToListAsync();
            db.Testimonials.AddRange(
                new Testimonial { AgentId = agents[2].Id, ClientName = "J. Brown", Headline = "Çok şanslıydım", Location = "Forest Hills", Date = new DateTime(2024, 6, 1), IsApproved = true, Body = "İlk kez ev alıyordum ve hiçbir şey bilmiyordum; danışmanım her adımda yanımdaydı. Co-op başvurumu hazırladı, sorularıma hep hızlı cevap verdi." },
                new Testimonial { AgentId = agents[1].Id, ClientName = "Robert K.", Headline = "Dairem çok hızlı ve istenen fiyatın üzerinde satıldı", Location = "Upper East Side", Date = new DateTime(2025, 3, 1), IsApproved = true, Body = "Avukatım ve alıcı tarafıyla sürekli iletişimde kaldılar, son dakika pazarlığında da büyük destek oldular." },
                new Testimonial { AgentId = agents[0].Id, ClientName = "Mr. & Mrs. L.", Headline = "Amerika'daki ilk evimize kavuştuk!", Location = "Queens", Date = new DateTime(2023, 9, 1), IsApproved = true, Body = "Ofise girdiğimiz andan kapanışa kadar harika bir deneyimdi. Ekip her soruyu sabırla yanıtladı." },
                new Testimonial { ClientName = "Anna K.", Headline = "Beyaz eldiven hizmet", Location = "Manhattan", Date = new DateTime(2022, 6, 1), IsApproved = true, Body = "Daha önce çalıştığım hiçbir ajans bu kadar özenli değildi. Bana zaman ve para kazandıran değerli ipuçları verdiler." });
            await db.SaveChangesAsync();
        }

        if (!await db.Posts.AnyAsync())
        {
            db.Posts.AddRange(
                new Post { Category = PostCategory.Blog, Title = "Co-op kurulu başvuru süreci nasıl işler?", Slug = "co-op-kurulu-basvuru-sureci", Author = "Editör", PublishedOn = new DateTime(2026, 3, 6), Summary = "New York'ta co-op satın alırken kurul başvurusunun aşamaları.", Body = "<p>Co-op satın almak, klasik bir tapu devrinden farklıdır: alıcı, bina kurulunun onayını almak zorundadır. Bu yazıda başvuru paketinde istenen belgeleri ve mülakat sürecini özetliyoruz.</p>" },
                new Post { Category = PostCategory.Blog, Title = "CEMA nedir ve alıcıya nasıl para kazandırır?", Slug = "cema-nedir", Author = "Editör", PublishedOn = new DateTime(2025, 8, 23), Summary = "Konsolidasyon, Uzatma ve Değişiklik Sözleşmesi ile ipotek vergisini azaltmak.", Body = "<p>New York eyaletinde ipotek kaydedilirken ipotek vergisi alınır. Belirli koşullarda CEMA ile bu vergi azaltılabilir veya hiç ödenmeyebilir.</p>" },
                new Post { Category = PostCategory.Blog, Title = "Eyaletlere göre yıllık değer artışı", Slug = "eyaletlere-gore-deger-artisi", Author = "Editör", PublishedOn = new DateTime(2025, 7, 13), Summary = "Tekrarlı satış verilerine göre mülk değer artış oranları.", Body = "<p>Değer artışı, aynı mülklerin tekrar satışlarına göre hesaplanır. Artan fiyatlar genellikle talebin yükseldiğini gösterir.</p>" },
                new Post { Category = PostCategory.Blog, Title = "Satılık evinizi rakiplerinden nasıl öne çıkarırsınız?", Slug = "evinizi-one-cikarin", Author = "Editör", PublishedOn = new DateTime(2025, 4, 23), Summary = "Lüks konut satıcıları için pratik ipuçları.", Body = "<p>Profesyonel fotoğraf, doğru fiyatlama ve sahneleme (home staging) alıcıların ilgisini çeker.</p>" },
                new Post { Category = PostCategory.Blog, Title = "Satışta neler kalır, neler gider? Demirbaş ve eşyalar", Slug = "demirbas-ve-esyalar", Author = "Editör", PublishedOn = new DateTime(2024, 7, 21), Summary = "Alıcı ile satıcı arasında en sık yaşanan anlaşmazlık.", Body = "<p>Sözleşmede hangi eşyaların satışa dahil olduğunu açıkça yazmak, kapanışta sürprizleri önler.</p>" },
                new Post { Category = PostCategory.News, Title = "Ofisimiz Florida'da yeni şube açtı", Slug = "florida-subesi", Author = "Yönetim", PublishedOn = new DateTime(2026, 1, 10), Summary = "North Port / Port Charlotte bölgesinde hizmet vermeye başladık.", Body = "<p>Güneybatı Florida'daki müşterilerimize artık yerel bir ofisten hizmet veriyoruz.</p>" });
            await db.SaveChangesAsync();
        }

        if (!await db.ContentPages.AnyAsync())
        {
            db.ContentPages.AddRange(DefaultPages());
            await db.SaveChangesAsync();
        }
    }

    private static async Task SeedIdentityAsync(IServiceProvider sp, IConfiguration config)
    {
        var roles = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var users = sp.GetRequiredService<UserManager<ApplicationUser>>();
        if (!await roles.RoleExistsAsync(AdminRole)) await roles.CreateAsync(new IdentityRole(AdminRole));

        var email = config["Admin:Email"];
        var password = config["Admin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return;

        var admin = await users.FindByEmailAsync(email);
        if (admin is null)
        {
            admin = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, FirstName = "Site", LastName = "Yöneticisi" };
            var result = await users.CreateAsync(admin, password);
            if (!result.Succeeded)
                throw new InvalidOperationException("Admin kullanıcısı oluşturulamadı: " + string.Join("; ", result.Errors.Select(e => e.Description)));
        }
        if (!await users.IsInRoleAsync(admin, AdminRole)) await users.AddToRoleAsync(admin, AdminRole);
    }

    private static IEnumerable<Listing> GenerateListings(List<Neighborhood> hoods, List<Agent> agents)
    {
        var rnd = new Random(42);
        string[] streets = { "East 87th Street", "West 57th Street", "Fifth Avenue", "Park Avenue", "Lexington Avenue", "West Street", "Ocean Parkway", "Queens Boulevard", "Bedford Avenue", "Amsterdam Avenue", "Broadway", "Atlantic Avenue" };
        string[] brokers = { "Demo Realty Group", "Sample Brokerage LLC", "Example Homes" };
        var nyc = hoods.Where(h => h.Area!.Name is "Manhattan" or "Brooklyn" or "Queens" or "Bronx").ToList();
        var list = new List<Listing>();

        for (var i = 0; i < 48; i++)
        {
            var hood = nyc[rnd.Next(nyc.Count)];
            var rental = i % 2 == 1;
            var commercial = i % 12 == 0;
            var beds = commercial ? 0 : rnd.Next(0, 6);
            var type = commercial ? (PropertyType)rnd.Next(10, 16) : (PropertyType)rnd.Next(1, 5);
            var price = rental ? 1800 + beds * 1100 + rnd.Next(0, 40) * 50 : 350_000 + beds * 280_000 + rnd.Next(0, 60) * 10_000;
            var company = i % 3 != 2;
            var status = i % 9 == 4 ? ListingStatus.ApplicationPending : i % 13 == 6 ? ListingStatus.InContract : ListingStatus.Active;

            var l = new Listing
            {
                Address = $"{rnd.Next(10, 999)} {streets[rnd.Next(streets.Length)]}",
                Unit = rnd.Next(3) == 0 ? null : $"{rnd.Next(1, 40)}{(char)('A' + rnd.Next(6))}",
                NeighborhoodId = hood.Id,
                AgentId = company ? agents[rnd.Next(agents.Count)].Id : null,
                ListingType = rental ? ListingType.Rental : ListingType.Sale,
                Status = status,
                PropertyType = type,
                Source = company ? ListingSource.Company : ListingSource.Mls,
                IsExclusive = company && rnd.Next(3) == 0,
                CourtesyOf = company ? null : brokers[rnd.Next(brokers.Length)],
                Price = price,
                Bedrooms = beds,
                Bathrooms = Math.Max(1, beds) + (rnd.Next(2) == 0 ? 0 : 0.5m),
                Families = type is PropertyType.Townhouse or PropertyType.House ? rnd.Next(1, 4) : null,
                SquareFeet = 450 + beds * 350 + rnd.Next(0, 300),
                MonthlyExpense = rental ? null : 600 + beds * 400,
                HasDoorman = rnd.Next(2) == 0,
                HasElevator = rnd.Next(3) != 0,
                HasLaundry = rnd.Next(2) == 0,
                HasPool = rnd.Next(8) == 0,
                HasOutdoorSpace = rnd.Next(3) == 0,
                HasParking = rnd.Next(4) == 0,
                LaundryInUnit = rnd.Next(3) == 0,
                NoFee = rental && rnd.Next(3) == 0,
                PrivateOutdoorSpace = rnd.Next(5) == 0,
                IsShortTerm = rental && i % 7 == 1,
                CentralAirConditioning = rnd.Next(2) == 0,
                CatsOk = rnd.Next(2) == 0,
                DogsOk = rnd.Next(3) == 0,
                OpenHouseAt = i % 5 == 0 ? DateTime.UtcNow.Date.AddDays(rnd.Next(1, 20)).AddHours(13) : null,
                AvailableOn = rental ? DateTime.UtcNow.Date.AddDays(rnd.Next(0, 60)) : null,
                ListedOn = DateTime.UtcNow.AddDays(-rnd.Next(1, 120)),
                Latitude = 40.70m + (decimal)rnd.NextDouble() * 0.12m,
                Longitude = -74.00m + (decimal)rnd.NextDouble() * 0.10m,
                Description = "<p>Bol ışık alan, yenilenmiş mutfağa ve geniş yaşam alanına sahip daire. Ulaşıma, parklara ve alışveriş noktalarına yürüme mesafesinde.</p>",
                Photos = Enumerable.Range(1, 3).Select(n => new ListingPhoto { Url = $"/img/listing-{(i + n) % 6 + 1}.svg", SortOrder = n }).ToList()
            };
            list.Add(l);
        }
        return list;
    }

    private static IEnumerable<ContentPage> DefaultPages() => new[]
    {
        Page("about", "Şirketimiz", "<p>Manhattan, Miami ve Güneybatı Florida'da danışmanları bulunan, tam hizmet veren butik bir emlak firmasıyız. Satışa, yeni proje pazarlamasına ve kiralamaya — konut ve ticari gayrimenkulde — eşit değer ve kaynak ayırıyoruz.</p><h4>Misyonumuz</h4><p>Müşterilerimize olağanüstü bir emlak deneyimi sunmak ve uzun soluklu ilişkiler kurmak.</p><h4>Vizyonumuz</h4><p>Müşterilerin alım, satım ve kiralama süreçlerinde mükemmel hizmet ve profesyonel rehberlik aldığı bir şirket.</p><h4>Değerlerimiz</h4><p>Her danışmanımızın önceliği müşterisinin refahıdır. Hizmetimiz tarafsızdır ve ırk, kültür, din ya da sosyal geçmiş ayırt etmeksizin herkese açıktır.</p>"),
        Page("cbb", "Sertifikalı Alıcı Brokeri", "<p>Alıcı brokeri olarak yalnızca alıcının çıkarlarını temsil ederiz: piyasa analizi, teklif stratejisi ve pazarlık.</p>"),
        Page("disclaimer", "Yasal Uyarı", "<p>Bu sitedeki bilgiler güvenilir kabul edilen kaynaklardan derlenmiştir ancak garanti edilmez. Fiyatlar ve müsaitlik önceden haber verilmeksizin değişebilir.</p>"),
        Page("relocation", "Taşınma Hizmetleri", "<p>Şehre yeni taşınanlara semt turu, okul bilgisi ve geçici konaklama konusunda destek veriyoruz.</p>"),
        Page("buyers", "Alıcı Rehberi", "<ol><li>Bütçenizi ve ön kredi onayınızı belirleyin.</li><li>Bir alıcı brokeri ile çalışın.</li><li>Teklif verin, avukatınızla sözleşmeyi inceleyin.</li><li>Co-op ise kurul başvurusunu hazırlayın.</li><li>Kapanış.</li></ol>"),
        Page("sellers", "Satıcı Rehberi", "<ol><li>Karşılaştırmalı piyasa analizi (CMA) ile fiyatı belirleyin.</li><li>Mülkü hazırlayın ve fotoğraflayın.</li><li>Pazarlama ve açık ev günleri.</li><li>Teklifleri değerlendirin ve kapanış.</li></ol>"),
        Page("renters", "Kiracı Rehberi", "<p>Başvuru için genellikle gelir belgesi, kredi raporu, kimlik ve referans mektupları istenir. Gelirinizin yıllık kiranın yaklaşık 40 katı olması beklenir.</p>"),
        Page("neighborhoods", "Semt Rehberi", "<p>Manhattan, Brooklyn, Queens ve Bronx semtleri hakkında kısa rehberler. İlan aramasında semt filtresini kullanarak bölgedeki ilanları görebilirsiniz.</p>"),
        Page("links", "Faydalı Bağlantılar", "<ul><li><a href=\"https://www.rebny.com/\" target=\"_blank\" rel=\"noopener\">Real Estate Board of New York</a></li><li><a href=\"https://www.nyc.gov/site/fairhousing/index.page\" target=\"_blank\" rel=\"noopener\">NYC Fair Housing</a></li></ul>"),
        Page("owners", "Mülkünü Bizimle Listele", "<p>Mülkünüzü satmak veya kiraya vermek mi istiyorsunuz? Aşağıdaki formu doldurun, danışmanlarımız sizinle iletişime geçsin.</p>"),
        Page("florida", "Florida Hakkında", "<p>Güneybatı Florida; ılıman iklimi, sahilleri ve uygun fiyatlı konutlarıyla hem yatırım hem emeklilik için popüler bir bölgedir.</p>"),
        Page("north-port", "North Port / Port Charlotte", "<p>North Port ve Port Charlotte, Sarasota ile Fort Myers arasında yer alan, hızla büyüyen yerleşim bölgeleridir.</p>"),
        Page("careers", "Kariyer", "<p>Ekibimize katılın! Lisanslı danışmanlara ve lisans kursuna devam eden adaylara açığız. Aşağıdaki mülakat başvuru formunu doldurun.</p>"),
    };

    private static ContentPage Page(string slug, string title, string body) => new() { Slug = slug, Title = title, Body = body };
}
