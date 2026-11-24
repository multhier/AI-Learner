using Kivilcim.Data;
using Kivilcim.Models;
using Kivilcim.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Varsayılan veritabanı MSSQL. Test için "Database:Provider" = "Sqlite" verilebilir.
var provider = builder.Configuration["Database:Provider"] ?? "SqlServer";
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
var useSqlite = provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (useSqlite)
        options.UseSqlite(connectionString);
    else
        options.UseSqlServer(connectionString);
});
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");

builder.Services.AddScoped<PostQueryService>();
builder.Services.AddSingleton<ImageStore>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (useSqlite)
        await db.Database.EnsureCreatedAsync();   // SQLite: migration yerine şemadan oluştur
    else
        await db.Database.MigrateAsync();         // MSSQL: migration'ları uygula

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

app.Run();
