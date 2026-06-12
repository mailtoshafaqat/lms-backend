using Lms.Modules.Platform.Infrastructure;
using Lms.Shared.Branding;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Platform.Application;

public sealed class InstituteBrandingReader : IInstituteBrandingReader
{
    private readonly PlatformDbContext _db;

    public InstituteBrandingReader(PlatformDbContext db) => _db = db;

    public async Task<InstituteBrandingSnapshot?> GetAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        if (tenant is null) return null;

        var settings = await _db.TenantSettings.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId, ct);

        var displayName = string.IsNullOrWhiteSpace(settings?.DisplayName)
            ? tenant.Name
            : settings.DisplayName.Trim();

        return new InstituteBrandingSnapshot(displayName, settings?.LogoUrl);
    }
}
