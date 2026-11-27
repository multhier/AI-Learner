using Kivilcim.Bll;
using Kivilcim.Dal;
using Kivilcim.Dal.Data;
using Kivilcim.Dal.Email;
using Kivilcim.Dal.Storage;
using Kivilcim.Core.Abstractions;
using Kivilcim.Web.Hubs;
using Kivilcim.Web.Realtime;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Veritabanı: varsayılan SQL Server (LocalDB). Test için "Database:Provider" = "Sqlite" verilebilir.
var provider = builder.Configuration["Database:Provider"] ?? "SqlServer";
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
var useSqlite = provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase);

// Soğan mimarisi: altyapı (Dal) ve uygulama (Bll) servisleri, sunum (Web) en dışta.
builder.Services.AddDal(connectionString, useSqlite);
builder.Services.AddBll();
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.Configure<StorageOptions>(o =>
    o.WebRootPath = builder.Environment.WebRootPath ?? Path.Combine(builder.Environment.ContentRootPath, "wwwroot"));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");
builder.Services.AddSignalR();
builder.Services.AddSingleton<IRealtimeNotifier, SignalRRealtimeNotifier>();

// Konteynerde yeniden başlatmada oturumlar düşmesin diye anahtarlar kalıcı klasörde tutulur.
var keysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrEmpty(keysPath))
{
    builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keysPath));
}

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    // Veritabanı yoksa oluşturulur (LocalDB dahil); migration'lar her açılışta uygulanır.
    if (useSqlite)
        await db.Database.EnsureCreatedAsync();
    else
        await db.Database.MigrateAsync();

    if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Database:SeedDemoData"))
    {
        await SeedData.EnsureSeededAsync(scope.ServiceProvider);
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();
