using Microsoft.AspNetCore.Http;

namespace Lms.Shared.Tenancy;

/// <summary>
/// Default tenant resolution. Phase 1: a single seeded tenant (from config / fallback).
/// Phase 2: resolve from subdomain/custom-domain or an authenticated tenant claim.
/// </summary>
public sealed class TenantContext : ITenantContext
{
    /// <summary>Platform/system scope for SuperAdmin accounts (not an institute tenant).</summary>
    public static readonly Guid SystemTenantId = Guid.Parse("00000000-0000-0000-0000-000000000000");

    /// <summary>Default demo institute tenant (dev seed).</summary>
    public static readonly Guid DefaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private readonly IHttpContextAccessor _accessor;

    public TenantContext(IHttpContextAccessor accessor) => _accessor = accessor;

    public Guid TenantId => ResolveTenantId();

    public bool HasTenant => TenantId != Guid.Empty;

    private Guid ResolveTenantId()
    {
        var http = _accessor.HttpContext;

        // Authenticated requests: JWT tenant_id claim wins.
        var claim = http?.User?.FindFirst("tenant_id")?.Value;
        if (Guid.TryParse(claim, out var fromClaim))
            return fromClaim;

        // Unauthenticated / pre-auth: subdomain or X-Tenant-Slug middleware.
        if (http?.Items.TryGetValue("ResolvedTenantId", out var resolved) == true
            && resolved is Guid tenantId)
            return tenantId;

        return DefaultTenantId;
    }
}
