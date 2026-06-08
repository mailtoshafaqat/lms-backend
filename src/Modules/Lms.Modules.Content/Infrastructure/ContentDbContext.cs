using Lms.Modules.Content.Domain;
using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Content.Infrastructure;

public sealed class ContentDbContext : DbContext
{
    private readonly ITenantContext _tenant;

    public ContentDbContext(DbContextOptions<ContentDbContext> options, ITenantContext tenant)
        : base(options)
    {
        _tenant = tenant;
    }

    public DbSet<Lecture> Lectures => Set<Lecture>();
    public DbSet<Note> Notes => Set<Note>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("content");

        builder.Entity<Lecture>(e =>
        {
            e.ToTable("Lectures");
            e.Property(l => l.Title).IsRequired().HasMaxLength(200);
            e.HasIndex(l => l.TopicId);
            e.HasQueryFilter(l => l.TenantId == _tenant.TenantId);
        });

        builder.Entity<Note>(e =>
        {
            e.ToTable("Notes");
            e.Property(n => n.Title).IsRequired().HasMaxLength(200);
            e.HasIndex(n => n.TopicId);
            e.HasQueryFilter(n => n.TenantId == _tenant.TenantId);
        });

        base.OnModelCreating(builder);
    }
}
