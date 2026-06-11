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
    public DbSet<MockExam> MockExams => Set<MockExam>();
    public DbSet<MockExamSection> MockExamSections => Set<MockExamSection>();
    public DbSet<MockExamTopic> MockExamTopics => Set<MockExamTopic>();
    public DbSet<MockExamAttempt> MockExamAttempts => Set<MockExamAttempt>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("assessments");

        builder.Entity<Quiz>(e =>
        {
            e.ToTable("Quizzes");
            e.Property(q => q.Title).IsRequired().HasMaxLength(200);
            e.HasIndex(q => q.TopicId);
            e.HasIndex(q => new { q.UnitId, q.Type });
            e.HasQueryFilter(q => q.TenantId == _tenant.TenantId);
            e.HasMany(q => q.Questions).WithOne(x => x.Quiz!).HasForeignKey(x => x.QuizId);
        });

        builder.Entity<Question>(e =>
        {
            e.ToTable("Questions");
            e.Property(q => q.Stem).IsRequired();
            e.Property(q => q.PyqExam).HasMaxLength(64);
            e.HasQueryFilter(q => q.TenantId == _tenant.TenantId);
        });

        builder.Entity<Attempt>(e =>
        {
            e.ToTable("Attempts");
            e.HasIndex(a => new { a.UserId, a.QuizId });
            e.HasQueryFilter(a => a.TenantId == _tenant.TenantId);
        });

        builder.Entity<MockExam>(e =>
        {
            e.ToTable("MockExams");
            e.Property(m => m.Title).IsRequired().HasMaxLength(200);
            e.Property(m => m.BundleTitle).IsRequired().HasMaxLength(200);
            e.Property(m => m.SubjectTitle).IsRequired().HasMaxLength(200);
            e.HasIndex(m => m.SubjectId);
            e.HasIndex(m => m.BundleId);
            e.HasQueryFilter(m => m.TenantId == _tenant.TenantId);
            e.Property(m => m.MarksPerCorrect).HasPrecision(10, 2);
            e.Property(m => m.PenaltyPerWrong).HasPrecision(10, 2);
            e.HasMany(m => m.Sections).WithOne(s => s.MockExam!).HasForeignKey(s => s.MockExamId);
            e.HasMany(m => m.Topics).WithOne(t => t.MockExam!).HasForeignKey(t => t.MockExamId);
        });

        builder.Entity<MockExamSection>(e =>
        {
            e.ToTable("MockExamSections");
            e.Property(s => s.Title).IsRequired().HasMaxLength(200);
            e.HasIndex(s => s.MockExamId);
            e.HasQueryFilter(s => s.TenantId == _tenant.TenantId);
            e.HasMany(s => s.Topics).WithOne(t => t.Section!).HasForeignKey(t => t.SectionId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<MockExamTopic>(e =>
        {
            e.ToTable("MockExamTopics");
            e.Property(t => t.TopicTitle).IsRequired().HasMaxLength(200);
            e.HasQueryFilter(t => t.TenantId == _tenant.TenantId);
        });

        builder.Entity<MockExamAttempt>(e =>
        {
            e.ToTable("MockExamAttempts");
            e.Property(a => a.Score).HasPrecision(10, 2);
            e.HasIndex(a => new { a.UserId, a.MockExamId });
            e.HasQueryFilter(a => a.TenantId == _tenant.TenantId);
        });

        base.OnModelCreating(builder);
    }
}
