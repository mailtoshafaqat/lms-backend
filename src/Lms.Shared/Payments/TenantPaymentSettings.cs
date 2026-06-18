namespace Lms.Shared.Payments;

[Flags]
public enum PaymentGatewayFlags
{
    None = 0,
    Manual = 1,
    Stripe = 2,
    JazzCash = 4,
    Easypaisa = 8
}

[Flags]
public enum EnrollmentModes
{
    AdminOnly = 0,
    SelfEnrollFree = 1,
    ManualPayment = 2,
    OnlineCheckout = 4
}

[Flags]
public enum PaymentsConfiguredFlags
{
    None = 0,
    Manual = 1,
    Stripe = 2,
    JazzCash = 4,
    Easypaisa = 8
}

public enum PaymentGateway
{
    Manual,
    Stripe,
    JazzCash,
    Easypaisa
}

public enum PaymentStatus
{
    Pending,
    AwaitingApproval,
    Processing,
    Paid,
    Failed,
    Cancelled,
    Refunded
}

public sealed record TenantPaymentSettings(
    string Country,
    string Currency,
    PaymentGatewayFlags AllowedPaymentGateways,
    EnrollmentModes EnrollmentModes,
    PaymentsConfiguredFlags PaymentsConfigured,
    string? ManualPaymentInstructions,
    string? StripePublishableKey,
    string? StripeSecretKey,
    string? StripeWebhookSecret,
    string? JazzCashMerchantId,
    string? JazzCashPassword,
    string? JazzCashHashKey,
    string? JazzCashReturnUrl,
    string? EasypaisaStoreId,
    string? EasypaisaHashKey,
    string? EasypaisaCredentials);

public interface ITenantPaymentSettingsProvider
{
    Task<TenantPaymentSettings?> GetAsync(CancellationToken ct = default);

    Task<TenantPaymentSettings?> GetForTenantAsync(Guid tenantId, CancellationToken ct = default);
}
