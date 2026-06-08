using Lms.Modules.Platform.Domain;
using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Lms.Modules.Platform.Infrastructure;

public sealed class TenantResolver : ITenantResolver
{
    private readonly PlatformDbContext _db;
    private readonly string _baseHost;

    public TenantResolver(PlatformDbContext db, IConfiguration configuration)
    {
        _db = db;
        _baseHost = configuration["Tenancy:BaseHost"] ?? "localhost";
    }

    public async Task<string?> ResolveSlugFromHostAsync(string? host, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(host)) return null;

        var normalized = host.Split(':')[0].Trim().ToLowerInvariant();
        var baseHost = _baseHost.Trim().ToLowerInvariant();

        // Custom apex domain (e.g. academy.com) takes precedence.
        var byDomain = await _db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.CustomDomain != null
                     && t.CustomDomain.ToLower() == normalized
                     && t.Status != TenantStatus.Suspended, ct);
        if (byDomain is not null) return byDomain.Slug;

        // demo.localhost → demo
        if (normalized.EndsWith($".{baseHost}", StringComparison.Ordinal))
        {
            var sub = normalized[..^(baseHost.Length + 1)];
            if (!string.IsNullOrWhiteSpace(sub) && sub != "www" && !sub.Contains('.'))
                return sub;
        }

        return null;
    }

    public async Task<Guid?> ResolveTenantIdBySlugAsync(string slug, CancellationToken ct = default)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        var tenant = await _db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Slug == normalized && t.Status != TenantStatus.Suspended, ct);
        return tenant?.Id;
    }
}
