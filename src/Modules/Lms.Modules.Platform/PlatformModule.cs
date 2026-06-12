using Lms.Modules.Platform.Application;
using Lms.Modules.Platform.Infrastructure;
using Lms.Shared.Branding;
using Lms.Shared.Email;
using Lms.Shared.Storage;
using Lms.Shared.Tenancy;
using Lms.Shared.Integrations;
using Lms.Shared.Mentor;
using Lms.Shared.Modules;
using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lms.Modules.Platform;

public sealed class PlatformModule : IModule
{
    public string Name => "Platform";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PlatformDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("Default"),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "platform")));

        services.AddScoped<IPlatformSettingsService, PlatformSettingsService>();
        services.AddScoped<ILandingPageService, LandingPageService>();
        services.AddScoped<ITenantResolver, TenantResolver>();
        services.AddScoped<ITenantAdminService, TenantAdminService>();
        services.AddScoped<ITenantFeaturesProvider, TenantFeaturesProvider>();
        services.AddScoped<ITenantModuleAccess, TenantModuleAccess>();
        services.AddScoped<ITenantEmailSettingsProvider, TenantEmailSettingsProvider>();
        services.AddScoped<ITenantZoomSettingsProvider, TenantZoomSettingsProvider>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IBrandedEmailRenderer, BrandedEmailRenderer>();
        services.AddScoped<ISyllabusMentorGate, SyllabusMentorGate>();
        services.AddScoped<IRequestIncidentService, RequestIncidentService>();
        services.AddScoped<ITenantStorageQuotaService, TenantStorageQuotaService>();
        services.AddScoped<IInstituteBrandingReader, InstituteBrandingReader>();
    }
}
