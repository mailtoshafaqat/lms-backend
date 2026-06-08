using Lms.Modules.LiveClasses.Domain;
using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.LiveClasses.Infrastructure;

public sealed class LiveClassesDbContext : DbContext
{
    private readonly ITenantContext _tenant;

    public LiveClassesDbContext(DbContextOptions<LiveClassesDbContext> options, ITenantContext tenant)
        : base(options)
    {
        _tenant = tenant;
    }

    public DbSet<LiveClass> LiveClasses => Set<LiveClass>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("live");

        builder.Entity<LiveClass>(e =>
        {
            e.ToTable("LiveClasses");
            e.Property(x => x.Title).IsRequired().HasMaxLength(200);
            e.Property(x => x.BundleTitle).IsRequired().HasMaxLength(200);
            e.Property(x => x.JoinUrl).HasMaxLength(1000);
            e.Property(x => x.StartUrl).HasMaxLength(1000);
            e.Property(x => x.MeetingId).HasMaxLength(64);
            e.Property(x => x.Passcode).HasMaxLength(64);
            e.HasIndex(x => new { x.BundleId, x.ScheduledStartUtc });
            e.HasQueryFilter(x => x.TenantId == _tenant.TenantId);
        });

        base.OnModelCreating(builder);
    }
}
