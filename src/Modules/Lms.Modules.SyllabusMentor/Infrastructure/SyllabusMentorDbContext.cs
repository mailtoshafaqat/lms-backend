using Lms.Modules.SyllabusMentor.Domain;
using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.SyllabusMentor.Infrastructure;

public sealed class SyllabusMentorDbContext : DbContext
{
    private readonly ITenantContext _tenant;

    public SyllabusMentorDbContext(DbContextOptions<SyllabusMentorDbContext> options, ITenantContext tenant)
        : base(options) => _tenant = tenant;

    public DbSet<KnowledgeChunk> KnowledgeChunks => Set<KnowledgeChunk>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("mentor");

        builder.Entity<KnowledgeChunk>(e =>
        {
            e.ToTable("KnowledgeChunks");
            e.Property(c => c.SourceType).HasMaxLength(32);
            e.Property(c => c.SourceTitle).HasMaxLength(300);
            e.Property(c => c.Text).IsRequired();
            e.HasIndex(c => new { c.TopicId, c.SubjectId });
            e.HasIndex(c => c.TenantId);
            e.HasQueryFilter(c => c.TenantId == _tenant.TenantId);
        });

        base.OnModelCreating(builder);
    }
}
