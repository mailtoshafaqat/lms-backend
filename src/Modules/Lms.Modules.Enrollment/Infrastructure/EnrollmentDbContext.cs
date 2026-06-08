using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;
using EnrollmentEntity = Lms.Modules.Enrollment.Domain.Enrollment;

namespace Lms.Modules.Enrollment.Infrastructure;

public sealed class EnrollmentDbContext : DbContext
{
    private readonly ITenantContext _tenant;

    public EnrollmentDbContext(DbContextOptions<EnrollmentDbContext> options, ITenantContext tenant)
        : base(options)
    {
        _tenant = tenant;
    }

    public DbSet<EnrollmentEntity> Enrollments => Set<EnrollmentEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("enrollment");

        builder.Entity<EnrollmentEntity>(e =>
        {
            e.ToTable("Enrollments");
            e.Ignore(x => x.IsActive);
            e.Property(x => x.BundleTitle).IsRequired().HasMaxLength(200);
            e.HasIndex(x => new { x.UserId, x.BundleId }).IsUnique();
            e.HasQueryFilter(x => x.TenantId == _tenant.TenantId);
        });

        base.OnModelCreating(builder);
    }
}
