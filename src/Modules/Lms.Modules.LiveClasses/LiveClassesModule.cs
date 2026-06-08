using Lms.Modules.LiveClasses.Application;
using Lms.Modules.LiveClasses.Infrastructure;
using Lms.Shared.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lms.Modules.LiveClasses;

public sealed class LiveClassesModule : IModule
{
    public string Name => "LiveClasses";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<LiveClassesDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("Default"),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "live")));

        services.AddScoped<ILiveClassService, LiveClassService>();
        services.AddHttpClient<IZoomMeetingService, ZoomMeetingService>();
    }
}
