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
    public DbSet<SubjectDefinition> SubjectDefinitions => Set<SubjectDefinition>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<SubjectTeacher> SubjectTeachers => Set<SubjectTeacher>();
    public DbSet<SubjectDefinitionTeacher> SubjectDefinitionTeachers => Set<SubjectDefinitionTeacher>();
    public DbSet<SubjectSharedUnit> SubjectSharedUnits => Set<SubjectSharedUnit>();

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

        builder.Entity<SubjectDefinition>(e =>
        {
            e.ToTable("SubjectDefinitions");
            e.Property(d => d.Code).IsRequired().HasMaxLength(80);
            e.Property(d => d.DisplayName).IsRequired().HasMaxLength(200);
            e.HasIndex(d => new { d.TenantId, d.Code }).IsUnique();
            e.HasQueryFilter(d => d.TenantId == _tenant.TenantId);
            e.HasMany(d => d.BatchSubjects).WithOne(s => s.SubjectDefinition)
                .HasForeignKey(s => s.SubjectDefinitionId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasMany(d => d.LibraryUnits).WithOne(u => u.SubjectDefinition)
                .HasForeignKey(u => u.SubjectDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Subject>(e =>
        {
            e.ToTable("Subjects");
            e.Property(s => s.Title).IsRequired().HasMaxLength(200);
            e.HasQueryFilter(s => s.TenantId == _tenant.TenantId);
            e.HasMany(s => s.Units).WithOne(u => u.Subject!).HasForeignKey(u => u.SubjectId);
            e.HasMany(s => s.SharedUnitLinks).WithOne(l => l.Subject!)
                .HasForeignKey(l => l.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);
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

        builder.Entity<SubjectDefinitionTeacher>(e =>
        {
            e.ToTable("SubjectDefinitionTeachers");
            e.HasIndex(a => new { a.SubjectDefinitionId, a.UserId }).IsUnique();
            e.HasIndex(a => a.UserId);
            e.HasQueryFilter(a => a.TenantId == _tenant.TenantId);
            e.HasOne(a => a.SubjectDefinition).WithMany(d => d.Teachers)
                .HasForeignKey(a => a.SubjectDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SubjectSharedUnit>(e =>
        {
            e.ToTable("SubjectSharedUnits");
            e.HasIndex(l => new { l.SubjectId, l.UnitId }).IsUnique();
            e.HasQueryFilter(l => l.TenantId == _tenant.TenantId);
            e.HasOne(l => l.Unit).WithMany().HasForeignKey(l => l.UnitId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        base.OnModelCreating(builder);
    }
}
