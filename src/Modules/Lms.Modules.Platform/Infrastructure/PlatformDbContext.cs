using Lms.Modules.Platform.Domain;
using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Platform.Infrastructure;

public sealed class PlatformDbContext : DbContext
{
    private readonly ITenantContext _tenant;

    public PlatformDbContext(DbContextOptions<PlatformDbContext> options, ITenantContext tenant)
        : base(options)
    {
        _tenant = tenant;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantSettings> TenantSettings => Set<TenantSettings>();
    public DbSet<LandingPage> LandingPages => Set<LandingPage>();
    public DbSet<PageSection> PageSections => Set<PageSection>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("platform");

        builder.Entity<Tenant>(e =>
        {
            e.ToTable("Tenants");
            e.HasKey(t => t.Id);
            e.Property(t => t.Name).IsRequired().HasMaxLength(200);
            e.Property(t => t.Slug).IsRequired().HasMaxLength(64);
            e.Property(t => t.CustomDomain).HasMaxLength(256);
            e.HasIndex(t => t.Slug).IsUnique();
            e.HasIndex(t => t.CustomDomain).IsUnique().HasFilter("[CustomDomain] IS NOT NULL");
        });

        builder.Entity<TenantSettings>(e =>
        {
            e.ToTable("TenantSettings");
            e.HasIndex(s => s.TenantId).IsUnique();
            e.Property(s => s.FromEmail).HasMaxLength(256);
            e.Property(s => s.FromName).HasMaxLength(200);
            e.Property(s => s.SmtpHost).HasMaxLength(256);
            e.Property(s => s.SmtpUser).HasMaxLength(256);
            e.Property(s => s.ZoomAccountId).HasMaxLength(128);
            e.Property(s => s.ZoomClientId).HasMaxLength(128);
            e.Property(s => s.DisplayName).HasMaxLength(200);
            e.Property(s => s.LogoUrl).HasMaxLength(1000);
            e.Property(s => s.PrimaryColor).HasMaxLength(16);
            e.Property(s => s.SupportEmail).HasMaxLength(256);
            e.Property(s => s.FaviconUrl).HasMaxLength(1000);
            e.HasQueryFilter(s => s.TenantId == _tenant.TenantId);
        });

        builder.Entity<LandingPage>(e =>
        {
            e.ToTable("LandingPages");
            e.HasIndex(p => p.TenantId).IsUnique();
            e.HasMany(p => p.Sections).WithOne(s => s.LandingPage!).HasForeignKey(s => s.LandingPageId);
            e.HasQueryFilter(p => p.TenantId == _tenant.TenantId);
        });

        builder.Entity<PageSection>(e =>
        {
            e.ToTable("PageSections");
            e.Property(s => s.SectionType).IsRequired().HasMaxLength(32);
            e.Property(s => s.ContentJson).IsRequired();
        });

        base.OnModelCreating(builder);
    }
}
