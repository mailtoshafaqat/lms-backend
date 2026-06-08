using Lms.Modules.Identity.Domain;
using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Identity.Infrastructure;

/// <summary>Identity module's own DbContext. Owns the "identity" schema so the module
/// is self-contained and independently migratable.</summary>
public sealed class IdentityDbContext : DbContext
{
    private readonly ITenantContext _tenant;

    public IdentityDbContext(DbContextOptions<IdentityDbContext> options, ITenantContext tenant)
        : base(options)
    {
        _tenant = tenant;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("identity");

        builder.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasKey(u => u.Id);
            e.Property(u => u.Email).IsRequired().HasMaxLength(256);
            e.Property(u => u.FullName).IsRequired().HasMaxLength(200);
            e.Property(u => u.Role).IsRequired().HasMaxLength(50);
            e.HasIndex(u => new { u.TenantId, u.Email }).IsUnique();
            e.HasMany(u => u.RefreshTokens).WithOne(r => r.User!).HasForeignKey(r => r.UserId);

            // Tenant isolation: rows are always scoped to the current tenant.
            e.HasQueryFilter(u => u.TenantId == _tenant.TenantId);
        });

        builder.Entity<RefreshToken>(e =>
        {
            e.ToTable("RefreshTokens");
            e.HasKey(r => r.Id);
            e.Property(r => r.Token).IsRequired().HasMaxLength(512);
            e.HasIndex(r => r.Token);
        });

        builder.Entity<PasswordResetToken>(e =>
        {
            e.ToTable("PasswordResetTokens");
            e.Property(r => r.Token).IsRequired().HasMaxLength(128);
            e.HasIndex(r => r.Token).IsUnique();
            e.Ignore(r => r.IsValid);
        });

        base.OnModelCreating(builder);
    }
}
