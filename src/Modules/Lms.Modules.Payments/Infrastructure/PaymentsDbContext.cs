using Lms.Modules.Payments.Domain;
using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Payments.Infrastructure;

public sealed class PaymentsDbContext : DbContext
{
    private readonly ITenantContext _tenant;

    public PaymentsDbContext(DbContextOptions<PaymentsDbContext> options, ITenantContext tenant)
        : base(options)
    {
        _tenant = tenant;
    }

    public DbSet<PaymentOrder> PaymentOrders => Set<PaymentOrder>();
    public DbSet<PaymentWebhookEvent> PaymentWebhookEvents => Set<PaymentWebhookEvent>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("payments");

        builder.Entity<PaymentOrder>(e =>
        {
            e.ToTable("PaymentOrders");
            e.Property(x => x.BundleTitle).IsRequired().HasMaxLength(200);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Currency).IsRequired().HasMaxLength(3);
            e.Property(x => x.ExternalPaymentId).HasMaxLength(256);
            e.Property(x => x.ExternalSessionId).HasMaxLength(256);
            e.Property(x => x.FailureReason).HasMaxLength(2000);
            e.Property(x => x.FailureCode).HasMaxLength(64);
            e.Property(x => x.StudentCountry).HasMaxLength(2);
            e.HasIndex(x => new { x.TenantId, x.UserId, x.BundleId, x.Status });
            e.HasIndex(x => x.ExternalSessionId);
            e.HasQueryFilter(x => x.TenantId == _tenant.TenantId);
        });

        builder.Entity<PaymentWebhookEvent>(e =>
        {
            e.ToTable("PaymentWebhookEvents");
            e.Property(x => x.EventType).IsRequired().HasMaxLength(128);
            e.Property(x => x.ExternalEventId).IsRequired().HasMaxLength(256);
            e.Property(x => x.ErrorMessage).HasMaxLength(2000);
            e.HasIndex(x => new { x.Gateway, x.ExternalEventId }).IsUnique();
        });

        base.OnModelCreating(builder);
    }
}
