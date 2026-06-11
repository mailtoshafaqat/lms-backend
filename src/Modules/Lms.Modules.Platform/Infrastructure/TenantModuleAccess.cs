using Lms.Shared.Tenancy;

namespace Lms.Modules.Platform.Infrastructure;

public sealed class TenantModuleAccess : ITenantModuleAccess
{
    private readonly ITenantContext _tenant;
    private readonly ITenantFeaturesProvider _features;

    public TenantModuleAccess(ITenantContext tenant, ITenantFeaturesProvider features)
    {
        _tenant = tenant;
        _features = features;
    }

    public Task<TenantFeatures?> GetCurrentAsync(CancellationToken ct = default) =>
        _features.GetAsync(_tenant.TenantId, ct);

    public async Task<bool> IsEnabledAsync(ProductModule module, CancellationToken ct = default)
    {
        var features = await GetCurrentAsync(ct);
        return ProductModuleGate.IsEnabled(features, module);
    }
}
