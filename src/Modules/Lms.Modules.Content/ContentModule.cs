using Lms.Modules.Content.Application;
using Lms.Shared.Content;
using Lms.Modules.Content.Infrastructure;
using Lms.Shared.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lms.Modules.Content;

public sealed class ContentModule : IModule
{
    public string Name => "Content";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ContentDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("Default"),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "content")));

        services.AddScoped<IContentService, ContentService>();
        services.AddScoped<IContentAdminService, ContentAdminService>();
        services.AddScoped<IVideoLibraryService, VideoLibraryService>();
        services.AddScoped<IContentNotesReader, ContentNotesReader>();
        services.AddScoped<ILectureWriter, LectureWriter>();
        services.AddScoped<ILectureCatalog, LectureCatalog>();
    }
}
