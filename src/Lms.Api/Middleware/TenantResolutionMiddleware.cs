using Lms.Shared.Tenancy;

namespace Lms.Api.Middleware;

/// <summary>
/// Resolves tenant from subdomain (demo.localhost) or X-Tenant-Slug header for unauthenticated requests.
/// JWT tenant_id claim still takes precedence inside TenantContext when authenticated.
/// </summary>
public sealed class TenantResolutionMiddleware
{
    public const string TenantIdItemKey = "ResolvedTenantId";
    public const string TenantSlugItemKey = "ResolvedTenantSlug";
    public const string TenantSlugHeader = "X-Tenant-Slug";

    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ITenantResolver resolver)
    {
        string? slug = null;

        if (context.Request.Headers.TryGetValue(TenantSlugHeader, out var headerSlug)
            && !string.IsNullOrWhiteSpace(headerSlug))
        {
            slug = headerSlug.ToString().Trim().ToLowerInvariant();
        }
        else
        {
            slug = await resolver.ResolveSlugFromHostAsync(context.Request.Host.Host, context.RequestAborted);
        }

        if (!string.IsNullOrWhiteSpace(slug))
        {
            var tenantId = await resolver.ResolveTenantIdBySlugAsync(slug, context.RequestAborted);
            if (tenantId is not null)
            {
                context.Items[TenantIdItemKey] = tenantId.Value;
                context.Items[TenantSlugItemKey] = slug;
            }
        }

        await _next(context);
    }
}
