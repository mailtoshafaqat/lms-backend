using Lms.Shared.Payments;
using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Platform.Infrastructure;

public sealed class TenantPaymentSettingsProvider : ITenantPaymentSettingsProvider
{
    private readonly PlatformDbContext _db;
    private readonly ITenantContext _tenant;

    public TenantPaymentSettingsProvider(PlatformDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public Task<TenantPaymentSettings?> GetAsync(CancellationToken ct = default) =>
        GetForTenantAsync(_tenant.TenantId, ct);

    public async Task<TenantPaymentSettings?> GetForTenantAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        if (tenant is null) return null;

        var s = await _db.TenantSettings.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, ct);

        var configured = PaymentsConfiguredFlags.None;
        if (s?.ManualPaymentEnabled == true) configured |= PaymentsConfiguredFlags.Manual;
        if (s?.StripeEnabled == true
            && !string.IsNullOrEmpty(s.StripeSecretKey)
            && !string.IsNullOrEmpty(s.StripePublishableKey))
            configured |= PaymentsConfiguredFlags.Stripe;
        if (s?.JazzCashEnabled == true
            && !string.IsNullOrEmpty(s.JazzCashMerchantId)
            && !string.IsNullOrEmpty(s.JazzCashPassword)
            && !string.IsNullOrEmpty(s.JazzCashHashKey))
            configured |= PaymentsConfiguredFlags.JazzCash;
        if (s?.EasypaisaEnabled == true
            && !string.IsNullOrEmpty(s.EasypaisaStoreId)
            && !string.IsNullOrEmpty(s.EasypaisaHashKey))
            configured |= PaymentsConfiguredFlags.Easypaisa;

        return new TenantPaymentSettings(
            tenant.Country,
            tenant.Currency,
            tenant.AllowedPaymentGateways,
            tenant.EnrollmentModes,
            configured,
            s?.ManualPaymentInstructions,
            s?.StripePublishableKey,
            s?.StripeSecretKey,
            s?.StripeWebhookSecret,
            s?.JazzCashMerchantId,
            s?.JazzCashPassword,
            s?.JazzCashHashKey,
            s?.JazzCashReturnUrl,
            s?.EasypaisaStoreId,
            s?.EasypaisaHashKey,
            s?.EasypaisaCredentials);
    }
}
