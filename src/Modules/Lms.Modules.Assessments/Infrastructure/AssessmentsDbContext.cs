using Lms.Modules.Assessments.Domain;
using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Assessments.Infrastructure;

public sealed class AssessmentsDbContext : DbContext
{
    private readonly ITenantContext _tenant;

    public AssessmentsDbContext(DbContextOptions<AssessmentsDbContext> options, ITenantContext tenant)
        : base(options)
    {
        _tenant = tenant;
    }

    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Attempt> Attempts => Set<Attempt>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("assessments");

        builder.Entity<Quiz>(e =>
        {
            e.ToTable("Quizzes");
            e.Property(q => q.Title).IsRequired().HasMaxLength(200);
            e.HasIndex(q => q.TopicId);
            e.HasQueryFilter(q => q.TenantId == _tenant.TenantId);
            e.HasMany(q => q.Questions).WithOne(x => x.Quiz!).HasForeignKey(x => x.QuizId);
        });

        builder.Entity<Question>(e =>
        {
            e.ToTable("Questions");
            e.Property(q => q.Stem).IsRequired();
            e.HasQueryFilter(q => q.TenantId == _tenant.TenantId);
        });

        builder.Entity<Attempt>(e =>
        {
            e.ToTable("Attempts");
            e.HasIndex(a => new { a.UserId, a.QuizId });
            e.HasQueryFilter(a => a.TenantId == _tenant.TenantId);
        });

        base.OnModelCreating(builder);
    }
}
