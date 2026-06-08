namespace Lms.Shared.Entities;

/// <summary>
/// Marks an entity as belonging to a tenant. The TenantId is stamped from day one
/// (even in the single-tenant MVP) so multi-tenant isolation in Phase 2 is a switch,
/// not a re-architecture.
/// </summary>
public interface ITenantOwned
{
    Guid TenantId { get; set; }
}
