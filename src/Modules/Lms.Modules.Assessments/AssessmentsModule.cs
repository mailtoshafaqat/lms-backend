using Lms.Modules.Assessments.Application;
using Lms.Shared.Assessments;
using Lms.Modules.Assessments.Infrastructure;
using Lms.Shared.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lms.Modules.Assessments;

public sealed class AssessmentsModule : IModule
{
    public string Name => "Assessments";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AssessmentsDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("Default"),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "assessments")));

        services.AddScoped<IQuizService, QuizService>();
        services.AddScoped<IQuizAdminService, QuizAdminService>();
        services.AddScoped<IMockExamService, MockExamService>();
        services.AddScoped<IMockExamAdminService, MockExamAdminService>();
        services.AddScoped<QuizBatchNotifier>();
        services.AddScoped<IQuestionReader, QuestionReader>();
    }
}
