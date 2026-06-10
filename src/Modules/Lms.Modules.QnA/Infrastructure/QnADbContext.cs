using Lms.Modules.QnA.Domain;
using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.QnA.Infrastructure;

public sealed class QnADbContext : DbContext
{
    private readonly ITenantContext _tenant;

    public QnADbContext(DbContextOptions<QnADbContext> options, ITenantContext tenant)
        : base(options)
    {
        _tenant = tenant;
    }

    public DbSet<DoubtThread> DoubtThreads => Set<DoubtThread>();
    public DbSet<DoubtMessage> DoubtMessages => Set<DoubtMessage>();
    public DbSet<DoubtReplyTemplate> DoubtReplyTemplates => Set<DoubtReplyTemplate>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("qna");

        builder.Entity<DoubtThread>(e =>
        {
            e.ToTable("DoubtThreads");
            e.Property(t => t.SubjectTitle).IsRequired().HasMaxLength(200);
            e.Property(t => t.BundleTitle).IsRequired().HasMaxLength(200);
            e.Property(t => t.StudentName).IsRequired().HasMaxLength(200);
            e.Property(t => t.TopicTitle).HasMaxLength(200);
            e.Property(t => t.Title).IsRequired().HasMaxLength(120);
            e.Property(t => t.Status).HasConversion<string>().HasMaxLength(16);
            e.HasIndex(t => t.StudentUserId);
            e.HasIndex(t => t.SubjectId);
            e.HasIndex(t => new { t.Status, t.UpdatedAt });
            e.HasQueryFilter(t => t.TenantId == _tenant.TenantId);
            e.HasMany(t => t.Messages).WithOne(m => m.Thread!).HasForeignKey(m => m.ThreadId);
        });

        builder.Entity<DoubtMessage>(e =>
        {
            e.ToTable("DoubtMessages");
            e.Property(m => m.AuthorName).IsRequired().HasMaxLength(200);
            e.Property(m => m.AuthorRole).IsRequired().HasMaxLength(32);
            e.Property(m => m.Body).IsRequired();
            e.HasIndex(m => m.ThreadId);
            e.HasQueryFilter(m => m.TenantId == _tenant.TenantId);
        });

        builder.Entity<DoubtReplyTemplate>(e =>
        {
            e.ToTable("DoubtReplyTemplates");
            e.Property(t => t.Title).IsRequired().HasMaxLength(120);
            e.Property(t => t.Body).IsRequired();
            e.HasQueryFilter(t => t.TenantId == _tenant.TenantId);
        });

        base.OnModelCreating(builder);
    }
}
