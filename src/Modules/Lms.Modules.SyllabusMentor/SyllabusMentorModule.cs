using Lms.Modules.SyllabusMentor.Application;
using Lms.Modules.SyllabusMentor.Infrastructure;
using Lms.Shared.Content;
using Lms.Shared.Events;
using Lms.Shared.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lms.Modules.SyllabusMentor;

public sealed class SyllabusMentorModule : IModule
{
    public string Name => "SyllabusMentor";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SyllabusMentorOptions>(configuration.GetSection(SyllabusMentorOptions.SectionName));
        services.AddHttpClient();

        services.AddDbContext<SyllabusMentorDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("Default"),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "mentor")));

        services.AddScoped<NoteTextExtractor>();
        services.AddScoped<ISyllabusMentorService, SyllabusMentorService>();
        services.AddScoped<IEventHandler<NoteContentChangedEvent>, NoteIngestHandler>();
    }
}
