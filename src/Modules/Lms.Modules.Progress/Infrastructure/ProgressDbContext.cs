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
    public DbSet<Bookmark> Bookmarks => Set<Bookmark>();
    public DbSet<LectureWatchProgress> LectureWatchProgress => Set<LectureWatchProgress>();
    public DbSet<CompletionCertificate> CompletionCertificates => Set<CompletionCertificate>();
    public DbSet<CertificateTemplate> CertificateTemplates => Set<CertificateTemplate>();

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

        builder.Entity<Bookmark>(e =>
        {
            e.ToTable("Bookmarks");
            e.Property(b => b.TargetType).IsRequired().HasMaxLength(32);
            e.Property(b => b.Title).IsRequired().HasMaxLength(300);
            e.Property(b => b.Subtitle).HasMaxLength(300);
            e.HasIndex(b => new { b.UserId, b.TargetType, b.TargetId }).IsUnique();
            e.HasQueryFilter(b => b.TenantId == _tenant.TenantId);
        });

        builder.Entity<LectureWatchProgress>(e =>
        {
            e.ToTable("LectureWatchProgress");
            e.HasIndex(p => new { p.UserId, p.LectureId }).IsUnique();
            e.HasIndex(p => new { p.UserId, p.TopicId });
            e.HasQueryFilter(p => p.TenantId == _tenant.TenantId);
        });

        builder.Entity<CompletionCertificate>(e =>
        {
            e.ToTable("CompletionCertificates");
            e.Property(c => c.BundleTitle).IsRequired().HasMaxLength(200);
            e.Property(c => c.CertificateNumber).IsRequired().HasMaxLength(32);
            e.Property(c => c.StudentName).IsRequired().HasMaxLength(200);
            e.Property(c => c.InstituteName).IsRequired().HasMaxLength(200);
            e.HasIndex(c => new { c.UserId, c.BundleId }).IsUnique();
            e.HasIndex(c => new { c.TenantId, c.CertificateNumber }).IsUnique();
            e.HasQueryFilter(c => c.TenantId == _tenant.TenantId);
        });

        builder.Entity<CertificateTemplate>(e =>
        {
            e.ToTable("CertificateTemplates");
            e.Property(t => t.Title).IsRequired().HasMaxLength(200);
            e.Property(t => t.Subtitle).IsRequired().HasMaxLength(300);
            e.Property(t => t.BackgroundUrl).HasMaxLength(1000);
            e.Property(t => t.LogoUrl).HasMaxLength(1000);
            e.Property(t => t.SignatureUrl).HasMaxLength(1000);
            e.Property(t => t.SignatureLabel).IsRequired().HasMaxLength(120);
            e.Property(t => t.PrimaryColor).IsRequired().HasMaxLength(16);
            e.HasIndex(t => t.TenantId).IsUnique();
            e.HasQueryFilter(t => t.TenantId == _tenant.TenantId);
        });

        base.OnModelCreating(builder);
    }
}
