using Kivilcim.Core.Abstractions;
using Kivilcim.Dal.Data;
using Kivilcim.Dal.Email;
using Kivilcim.Dal.Identity;
using Kivilcim.Dal.Repositories;
using Kivilcim.Dal.Storage;
using Kivilcim.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Kivilcim.Dal;

public static class DependencyInjection
{
    /// <summary>Veritabanı, Identity, Unit of Work, dosya saklama ve e-posta altyapısını kaydeder.</summary>
    public static IServiceCollection AddDal(this IServiceCollection services, string connectionString, bool useSqlite)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            if (useSqlite)
                options.UseSqlite(connectionString, o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
            else
                options.UseSqlServer(connectionString, o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
        });

        services.AddDefaultIdentity<ApplicationUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.User.RequireUniqueEmail = true;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddErrorDescriber<TurkishIdentityErrorDescriber>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IQueryExecutor, EfQueryExecutor>();
        services.AddOptions<StorageOptions>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddOptions<EmailOptions>();
        services.AddTransient<IEmailSender, SmtpEmailSender>();
        return services;
    }
}
