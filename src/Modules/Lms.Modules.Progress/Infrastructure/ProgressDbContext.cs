using Lms.Modules.Progress.Domain;
using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Progress.Infrastructure;

public sealed class ProgressDbContext : DbContext
{
    private readonly ITenantContext _tenant;

    public ProgressDbContext(DbContextOptions<ProgressDbContext> options, ITenantContext tenant)
        : base(options)
    {
        _tenant = tenant;
    }

    public DbSet<QuizResult> QuizResults => Set<QuizResult>();
    public DbSet<MistakeEntry> MistakeEntries => Set<MistakeEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("progress");

        builder.Entity<QuizResult>(e =>
        {
            e.ToTable("QuizResults");
            e.Ignore(r => r.Percentage);
            e.Property(r => r.QuizTitle).IsRequired().HasMaxLength(200);
            e.HasIndex(r => new { r.UserId, r.QuizId });
            e.HasIndex(r => r.TenantId);
            e.HasQueryFilter(r => r.TenantId == _tenant.TenantId);
        });

        builder.Entity<MistakeEntry>(e =>
        {
            e.ToTable("MistakeEntries");
            e.Property(m => m.QuizTitle).HasMaxLength(200);
            e.HasIndex(m => new { m.UserId, m.QuestionId });
            e.HasQueryFilter(m => m.TenantId == _tenant.TenantId);
        });

        base.OnModelCreating(builder);
    }
}
