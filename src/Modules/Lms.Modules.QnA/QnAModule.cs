using Lms.Modules.QnA.Application;
using Lms.Modules.QnA.Infrastructure;
using Lms.Shared.QnA;
using Lms.Shared.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lms.Modules.QnA;

public sealed class QnAModule : IModule
{
    public string Name => "QnA";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<QnADbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("Default"),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "qna")));

        services.AddScoped<IDoubtService, DoubtService>();
        services.AddScoped<IDoubtTemplateService, DoubtTemplateService>();
        services.AddScoped<IDoubtSummaryReader, DoubtSummaryReader>();
    }
}
