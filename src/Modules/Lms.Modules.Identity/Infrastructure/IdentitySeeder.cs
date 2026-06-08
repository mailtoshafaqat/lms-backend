using Lms.Modules.Identity.Domain;
using Lms.Shared.Auth;
using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Identity.Infrastructure;

/// <summary>Seeds a default admin account for the default tenant (dev only) so the Admin CMS
/// can be used without manually promoting a user.</summary>
public static class IdentitySeeder
{
    public const string AdminEmail = "admin@demo.com";
    public const string AdminPassword = "Admin123!";
    public const string SuperAdminEmail = "superadmin@platform.com";
    public const string SuperAdminPassword = "SuperAdmin123!";

    public static async Task SeedAsync(IdentityDbContext db, IPasswordHasher hasher, CancellationToken ct = default)
    {
        if (!await db.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == SuperAdminEmail, ct))
        {
            db.Users.Add(new User
            {
                TenantId = TenantContext.SystemTenantId,
                Email = SuperAdminEmail,
                FullName = "Platform SuperAdmin",
                PasswordHash = hasher.Hash(SuperAdminPassword),
                Role = Roles.SuperAdmin,
                Provider = AuthProvider.Local
            });
        }

        if (!await db.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == AdminEmail, ct))
        {
            db.Users.Add(new User
            {
                TenantId = TenantContext.DefaultTenantId,
                Email = AdminEmail,
                FullName = "Demo Admin",
                PasswordHash = hasher.Hash(AdminPassword),
                Role = Roles.InstituteAdmin,
                Provider = AuthProvider.Local
            });
        }

        await db.SaveChangesAsync(ct);
    }
}
