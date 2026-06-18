using Lms.Shared.Common;
using Lms.Shared.Payments;

namespace Lms.Modules.Payments.Application;

public sealed record PaymentOrderDto(
    Guid Id,
    Guid BundleId,
    string BundleTitle,
    decimal Amount,
    string Currency,
    PaymentGateway Gateway,
    PaymentStatus Status,
    string? ExternalPaymentId,
    DateTime? PaidAt,
    Guid? EnrollmentId,
    DateTime CreatedAt,
    string? FailureReason);

public sealed record AvailableGatewayDto(
    PaymentGateway Gateway,
    string Label,
    string? Instructions);

public sealed record CheckoutRequest(Guid BundleId, PaymentGateway Gateway, string? StudentCountry);

public sealed record CheckoutFormPost(string ActionUrl, IReadOnlyDictionary<string, string> Fields);

public sealed record CheckoutResponse(
    Guid OrderId,
    PaymentGateway Gateway,
    PaymentStatus Status,
    string? CheckoutUrl,
    string? SessionId,
    string? Instructions,
    CheckoutFormPost? FormPost = null);

public sealed record PaymentWebhookResult(bool Success, Guid? OrderId, string? Message);

public sealed record ManualPaymentRequest(
    Guid BundleId,
    string TransactionRef,
    string? Note,
    string? StudentCountry);

public sealed record AdminPaymentOrderDto(
    Guid Id,
    Guid UserId,
    string? StudentFullName,
    string? StudentEmail,
    Guid BundleId,
    string BundleTitle,
    decimal Amount,
    string Currency,
    PaymentGateway Gateway,
    PaymentStatus Status,
    string? ExternalPaymentId,
    string? Note,
    string? MetadataJson,
    DateTime CreatedAt,
    DateTime? PaidAt);

public interface IPaymentGatewayResolver
{
    Task<IReadOnlyList<AvailableGatewayDto>> GetAvailableAsync(
        Guid bundleId, string? studentCountry, CancellationToken ct = default);
}

public interface IPaymentCheckoutService
{
    Task<Result<CheckoutResponse>> StartCheckoutAsync(
        Guid userId, CheckoutRequest request, CancellationToken ct = default);

    Task<Result<PaymentOrderDto>> SubmitManualAsync(
        Guid userId, ManualPaymentRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<PaymentOrderDto>> GetMyOrdersAsync(
        Guid userId, CancellationToken ct = default);
}

public interface IPaymentWebhookService
{
    Task ProcessStripeWebhookAsync(string json, string signatureHeader, CancellationToken ct = default);
    Task ProcessJazzCashWebhookAsync(IReadOnlyDictionary<string, string> form, CancellationToken ct = default);
    Task<PaymentWebhookResult> ProcessEasypaisaWebhookAsync(
        IReadOnlyDictionary<string, string> form, CancellationToken ct = default);
}

public sealed record AdminRecordManualPaymentRequest(
    Guid UserId,
    Guid BundleId,
    string TransactionRef,
    string? Note);

public interface IPaymentAdminService
{
    Task<IReadOnlyList<AdminPaymentOrderDto>> ListOrdersAsync(
        PaymentStatus? status, CancellationToken ct = default);

    Task<Result<PaymentOrderDto>> ApproveManualAsync(Guid orderId, CancellationToken ct = default);
    Task<Result<PaymentOrderDto>> RejectManualAsync(Guid orderId, string? reason, CancellationToken ct = default);

    /// <summary>Admin records an offline payment for an existing student and provisions enrollment immediately.</summary>
    Task<Result<PaymentOrderDto>> RecordManualForStudentAsync(
        AdminRecordManualPaymentRequest request, CancellationToken ct = default);

    Task<int> CountPendingManualAsync(CancellationToken ct = default);
}
