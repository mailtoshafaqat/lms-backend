namespace Lms.Shared.Entities;

/// <summary>Convenience base for tenant-scoped entities.</summary>
public abstract class TenantEntity : BaseEntity, ITenantOwned
{
    public Guid TenantId { get; set; }
}
