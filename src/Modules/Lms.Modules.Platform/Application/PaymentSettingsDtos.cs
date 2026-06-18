namespace Lms.Modules.Platform.Application;

using Lms.Shared.Payments;

public sealed record PaymentSettingsDto(
    int EnrollmentModes,
    string? ManualPaymentInstructions,
    ManualGatewaySettingsDto Manual,
    StripeGatewaySettingsDto Stripe,
    JazzCashGatewaySettingsDto JazzCash,
    EasypaisaGatewaySettingsDto Easypaisa);

public sealed record ManualGatewaySettingsDto(bool Enabled);

public sealed record StripeGatewaySettingsDto(
    bool Enabled,
    string PublishableKey,
    bool HasSecretKey,
    bool HasWebhookSecret);

public sealed record JazzCashGatewaySettingsDto(
    bool Enabled,
    string MerchantId,
    bool HasPassword,
    bool HasHashKey,
    string? ReturnUrl);

public sealed record EasypaisaGatewaySettingsDto(
    bool Enabled,
    string StoreId,
    bool HasHashKey,
    bool HasCredentials);

public sealed record UpdatePaymentSettingsRequest(
    int EnrollmentModes,
    string? ManualPaymentInstructions,
    bool ManualEnabled,
    bool StripeEnabled,
    string StripePublishableKey,
    string? StripeSecretKey,
    string? StripeWebhookSecret,
    bool JazzCashEnabled,
    string JazzCashMerchantId,
    string? JazzCashPassword,
    string? JazzCashHashKey,
    string? JazzCashReturnUrl,
    bool EasypaisaEnabled,
    string EasypaisaStoreId,
    string? EasypaisaHashKey,
    string? EasypaisaCredentials);
