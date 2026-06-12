using Lms.Shared.Auth;
using Lms.Shared.Events;
using Lms.Shared.Storage;
using Lms.Shared.Tenancy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lms.Shared;

public static class DependencyInjection
{
    /// <summary>Registers the shared kernel: tenant context, current user, event bus, file storage.</summary>
    public static IServiceCollection AddSharedKernel(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IEventBus, InMemoryEventBus>();

        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.SectionName));
        services.Configure<StorageQuotaOptions>(configuration.GetSection(StorageQuotaOptions.SectionName));
        services.AddSingleton<IFileStorage, LocalDiskFileStorage>();
        return services;
    }
}
