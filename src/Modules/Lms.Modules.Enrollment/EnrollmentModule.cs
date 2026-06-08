using Lms.Modules.Enrollment.Application;
using Lms.Modules.Enrollment.Infrastructure;
using Lms.Shared.Enrollments;
using Lms.Shared.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lms.Modules.Enrollment;

public sealed class EnrollmentModule : IModule
{
    public string Name => "Enrollment";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<EnrollmentDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("Default"),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "enrollment")));

        services.AddScoped<IEnrollmentService, EnrollmentService>();
        services.AddScoped<IEnrollmentWriter, EnrollmentWriter>();
        services.AddScoped<IEnrollmentReader, EnrollmentReader>();
    }
}
