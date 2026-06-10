using Lms.Modules.Courses.Domain;
using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Courses.Infrastructure;

public sealed class CoursesDbContext : DbContext
{
    private readonly ITenantContext _tenant;

    public CoursesDbContext(DbContextOptions<CoursesDbContext> options, ITenantContext tenant)
        : base(options)
    {
        _tenant = tenant;
    }

    public DbSet<Bundle> Bundles => Set<Bundle>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<SubjectTeacher> SubjectTeachers => Set<SubjectTeacher>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("courses");

        builder.Entity<Bundle>(e =>
        {
            e.ToTable("Bundles");
            e.Property(b => b.Title).IsRequired().HasMaxLength(200);
            e.Property(b => b.Price).HasColumnType("decimal(18,2)");
            e.HasQueryFilter(b => b.TenantId == _tenant.TenantId);
            e.HasMany(b => b.Subjects).WithOne(s => s.Bundle!).HasForeignKey(s => s.BundleId);
        });

        builder.Entity<Subject>(e =>
        {
            e.ToTable("Subjects");
            e.Property(s => s.Title).IsRequired().HasMaxLength(200);
            e.HasQueryFilter(s => s.TenantId == _tenant.TenantId);
            e.HasMany(s => s.Units).WithOne(u => u.Subject!).HasForeignKey(u => u.SubjectId);
        });

        builder.Entity<Unit>(e =>
        {
            e.ToTable("Units");
            e.Property(u => u.Title).IsRequired().HasMaxLength(200);
            e.HasQueryFilter(u => u.TenantId == _tenant.TenantId);
            e.HasMany(u => u.Topics).WithOne(t => t.Unit!).HasForeignKey(t => t.UnitId);
        });

        builder.Entity<Topic>(e =>
        {
            e.ToTable("Topics");
            e.Property(t => t.Title).IsRequired().HasMaxLength(200);
            e.HasQueryFilter(t => t.TenantId == _tenant.TenantId);
        });

        builder.Entity<SubjectTeacher>(e =>
        {
            e.ToTable("SubjectTeachers");
            e.HasIndex(a => new { a.SubjectId, a.UserId }).IsUnique();
            e.HasIndex(a => a.UserId);
            e.HasQueryFilter(a => a.TenantId == _tenant.TenantId);
            e.HasOne(a => a.Subject).WithMany().HasForeignKey(a => a.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        base.OnModelCreating(builder);
    }
}
