using Kivilcim.Bll.Abstractions;
using Kivilcim.Bll.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Kivilcim.Bll;

public static class DependencyInjection
{
    /// <summary>Uygulama servislerini kaydeder. Altyapı (Dal) ve sunum (Web) ayrıca kaydedilir.</summary>
    public static IServiceCollection AddBll(this IServiceCollection services)
    {
        services.AddScoped<IPostQueryService, PostQueryService>();
        services.AddScoped<IPostService, PostService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IBlogService, BlogService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IInboxService, InboxService>();
        return services;
    }
}
