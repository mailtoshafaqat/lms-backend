using Lms.Modules.Identity.Application;
using Lms.Modules.Identity.Infrastructure;
using Lms.Shared.Users;
using Lms.Shared.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lms.Modules.Identity;

public sealed class IdentityModule : IModule
{
    public string Name => "Identity";

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddDbContext<IdentityDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("Default"),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "identity")));

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAdminUserService, AdminUserService>();
        services.AddScoped<IInstituteAdminProvisioner, InstituteAdminProvisioner>();

        // Cross-module contract: expose user display names to other modules (e.g. Progress leaderboard).
        services.AddScoped<IUserDirectory, UserDirectory>();
    }
}
