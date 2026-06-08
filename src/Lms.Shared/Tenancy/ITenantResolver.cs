namespace Lms.Shared.Tenancy;

/// <summary>Resolves institute tenant from host header (subdomain) or explicit slug.</summary>
public interface ITenantResolver
{
    /// <summary>Extract tenant slug from request host (custom domain or subdomain).</summary>
    Task<string?> ResolveSlugFromHostAsync(string? host, CancellationToken ct = default);

    /// <summary>Look up tenant id by slug; null when not found or suspended.</summary>
    Task<Guid?> ResolveTenantIdBySlugAsync(string slug, CancellationToken ct = default);
}
