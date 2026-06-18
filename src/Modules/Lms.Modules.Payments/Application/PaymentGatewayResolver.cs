using Lms.Modules.Courses.Contracts;
using Lms.Shared.Payments;

namespace Lms.Modules.Payments.Application;

public sealed class PaymentGatewayResolver : IPaymentGatewayResolver
{
    private readonly IBundleCatalog _catalog;
    private readonly ITenantPaymentSettingsProvider _settings;

    public PaymentGatewayResolver(IBundleCatalog catalog, ITenantPaymentSettingsProvider settings)
    {
        _catalog = catalog;
        _settings = settings;
    }

    public async Task<IReadOnlyList<AvailableGatewayDto>> GetAvailableAsync(
        Guid bundleId, string? studentCountry, CancellationToken ct = default)
    {
        var bundle = await _catalog.GetBundleAsync(bundleId, ct);
        if (bundle is null || !bundle.IsPublished || bundle.Price <= 0)
            return Array.Empty<AvailableGatewayDto>();

        var settings = await _settings.GetAsync(ct);
        if (settings is null) return Array.Empty<AvailableGatewayDto>();

        var modes = settings.EnrollmentModes;
        if (!modes.HasFlag(EnrollmentModes.ManualPayment) && !modes.HasFlag(EnrollmentModes.OnlineCheckout))
            return Array.Empty<AvailableGatewayDto>();

        var country = NormalizeCountry(studentCountry ?? settings.Country);
        var isPk = country == "PK";
        var allowed = settings.AllowedPaymentGateways;
        var configured = settings.PaymentsConfigured;
        var list = new List<AvailableGatewayDto>();

        if (modes.HasFlag(EnrollmentModes.ManualPayment)
            && allowed.HasFlag(PaymentGatewayFlags.Manual)
            && configured.HasFlag(PaymentsConfiguredFlags.Manual))
        {
            list.Add(new AvailableGatewayDto(
                PaymentGateway.Manual,
                "Bank transfer / manual payment",
                settings.ManualPaymentInstructions));
        }

        if (modes.HasFlag(EnrollmentModes.OnlineCheckout))
        {
            if (allowed.HasFlag(PaymentGatewayFlags.Stripe)
                && configured.HasFlag(PaymentsConfiguredFlags.Stripe))
            {
                list.Add(new AvailableGatewayDto(PaymentGateway.Stripe, "Card (Stripe)", null));
            }

            if (isPk)
            {
                if (allowed.HasFlag(PaymentGatewayFlags.JazzCash)
                    && configured.HasFlag(PaymentsConfiguredFlags.JazzCash))
                {
                    list.Add(new AvailableGatewayDto(PaymentGateway.JazzCash, "JazzCash", null));
                }

                if (allowed.HasFlag(PaymentGatewayFlags.Easypaisa)
                    && configured.HasFlag(PaymentsConfiguredFlags.Easypaisa))
                {
                    list.Add(new AvailableGatewayDto(PaymentGateway.Easypaisa, "Easypaisa", null));
                }
            }
        }

        return list;
    }

    private static string NormalizeCountry(string? code) =>
        string.IsNullOrWhiteSpace(code) ? "PK" : code.Trim().ToUpperInvariant()[..Math.Min(2, code.Trim().Length)];
}
