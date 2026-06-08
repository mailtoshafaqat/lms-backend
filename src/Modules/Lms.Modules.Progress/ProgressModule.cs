using Lms.Modules.Assessments.Contracts;
using Lms.Modules.Progress.Application;
using Lms.Modules.Progress.Infrastructure;
using Lms.Shared.Events;
using Lms.Shared.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lms.Modules.Progress;

public sealed class ProgressModule : IModule
{
    public string Name => "Progress";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ProgressDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("Default"),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "progress")));

        services.AddScoped<IProgressService, ProgressService>();
        services.AddScoped<IMistakeDiaryService, MistakeDiaryService>();

        services.AddScoped<IEventHandler<QuizSubmittedEvent>, QuizSubmittedHandler>();
        services.AddScoped<IEventHandler<QuizSubmittedEvent>, MistakeDiaryHandler>();
    }
}
