using Lms.Shared.Entities;

namespace Lms.Modules.Enrollment.Domain;

/// <summary>A user's access grant to a bundle, valid until <see cref="ExpiresAt"/>.</summary>
public sealed class Enrollment : TenantEntity
{
    public Guid UserId { get; set; }
    public Guid BundleId { get; set; }
    public string BundleTitle { get; set; } = string.Empty;
    public decimal PricePaid { get; set; }
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }

    public bool IsActive => ExpiresAt > DateTime.UtcNow;
}
