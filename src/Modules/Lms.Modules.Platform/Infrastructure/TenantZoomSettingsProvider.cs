using Lms.Shared.Integrations;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Platform.Infrastructure;

public sealed class TenantZoomSettingsProvider : ITenantZoomSettingsProvider
{
    private readonly PlatformDbContext _db;

    public TenantZoomSettingsProvider(PlatformDbContext db) => _db = db;

    public async Task<TenantZoomSettings?> GetAsync(CancellationToken ct = default)
    {
        var s = await _db.TenantSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        if (s is null) return null;

        return new TenantZoomSettings(s.ZoomEnabled, s.ZoomAccountId, s.ZoomClientId, s.ZoomClientSecret);
    }
}
