using Lms.Shared.Auth;
using Lms.Shared.Configuration;
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
        services.Configure<AppUrlOptions>(configuration.GetSection(AppUrlOptions.SectionName));
        services.Configure<PaymentsOptions>(configuration.GetSection(PaymentsOptions.SectionName));
        services.AddSingleton<IAppUrls, AppUrls>();

        RegisterFileStorage(services, configuration);
        return services;
    }

    private static void RegisterFileStorage(IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration.GetSection(FileStorageOptions.SectionName).GetValue<string>("Provider") ?? "Local";
        switch (provider.Trim().ToLowerInvariant())
        {
            case "r2":
                services.AddSingleton<IFileStorage, R2FileStorage>();
                break;
            case "azure":
                services.AddSingleton<IFileStorage, AzureBlobFileStorage>();
                break;
            default:
                services.AddSingleton<IFileStorage, LocalDiskFileStorage>();
                break;
        }
    }
}
