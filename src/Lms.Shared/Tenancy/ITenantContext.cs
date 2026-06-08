namespace Lms.Shared.Tenancy;

/// <summary>
/// Resolves the current tenant for a request. In Phase 1 a default tenant is used;
/// in Phase 2 the tenant is resolved from the host/subdomain. Modules read TenantId
/// from here and never hard-code it.
/// </summary>
public interface ITenantContext
{
    Guid TenantId { get; }
    bool HasTenant { get; }
}
