using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebKoy.Web.Services;

namespace WebKoy.Web.Data;

public static class DbInitializer
{
    public const string AdminRole = "Admin";

    public static async Task InitializeAsync(IServiceProvider services, IConfiguration config)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (config.GetValue("Database:MigrateOnStartup", true) && db.Database.IsRelational())
            await db.Database.MigrateAsync();

        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roles.RoleExistsAsync(AdminRole))
            await roles.CreateAsync(new IdentityRole(AdminRole));

        // Platform:AdminEmails listesindeki kayıtlı kullanıcılar yönetici yapılır.
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var opt = scope.ServiceProvider.GetRequiredService<IOptions<PlatformOptions>>().Value;
        foreach (var email in opt.AdminEmails)
        {
            var user = await users.FindByEmailAsync(email);
            if (user is not null && !await users.IsInRoleAsync(user, AdminRole))
                await users.AddToRoleAsync(user, AdminRole);
        }
    }
}
