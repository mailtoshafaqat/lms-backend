using Lms.Modules.Courses.Application;
using Lms.Modules.Courses.Contracts;
using Lms.Modules.Courses.Infrastructure;
using Lms.Shared.Courses;
using Lms.Shared.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lms.Modules.Courses;

public sealed class CoursesModule : IModule
{
    public string Name => "Courses";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CoursesDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("Default"),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "courses")));

        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<ICourseAdminService, CourseAdminService>();
        services.AddScoped<ICourseScopeReader, CourseScopeReader>();
        services.AddScoped<ISubjectAccessService, SubjectAccessService>();
        services.AddScoped<IEnrolledSubjectsReader, EnrolledSubjectsReader>();

        // Cross-module contract: lets other modules (Enrollment) read bundle summaries.
        services.AddScoped<IBundleCatalog, BundleCatalog>();
    }
}
