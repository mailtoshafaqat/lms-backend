namespace Lms.Shared.Tenancy;

public interface ITenantModuleAccess
{
    Task<TenantFeatures?> GetCurrentAsync(CancellationToken ct = default);
    Task<bool> IsEnabledAsync(ProductModule module, CancellationToken ct = default);
}
