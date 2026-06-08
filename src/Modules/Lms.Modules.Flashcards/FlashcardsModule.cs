using Lms.Modules.Flashcards.Application;
using Lms.Modules.Flashcards.Infrastructure;
using Lms.Shared.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lms.Modules.Flashcards;

public sealed class FlashcardsModule : IModule
{
    public string Name => "Flashcards";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<FlashcardsDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("Default"),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "flashcards")));

        services.AddScoped<IFlashcardService, FlashcardService>();
        services.AddScoped<IFlashcardAdminService, FlashcardAdminService>();
    }
}
