using Lms.Modules.Platform.Domain;
using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Platform.Infrastructure;

public sealed class TenantFeaturesProvider : ITenantFeaturesProvider
{
    private readonly PlatformDbContext _db;

    public TenantFeaturesProvider(PlatformDbContext db) => _db = db;

    public async Task<TenantFeatures?> GetAsync(Guid tenantId, CancellationToken ct = default)
    {
        if (tenantId == TenantContext.SystemTenantId) return null;

        var t = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(x => x.Id == tenantId, ct);
        return t is null ? null : Map(t);
    }

    internal static TenantFeatures Map(Tenant t) => new(
        t.Id, t.Name, t.Slug, t.Status, t.Plan,
        t.LiveClassesEnabled, t.ZoomMode, t.PaymentMode,
        t.AllowStudentSelfEnroll, t.AllowAdminCreateStudent,
        t.BundlePriceEditEnabled, t.McqBulkImportEnabled);
}
