using Lms.Shared.Entities;
using Lms.Shared.Payments;

namespace Lms.Modules.Payments.Domain;

public sealed class PaymentOrder : TenantEntity
{
    public Guid UserId { get; set; }
    public Guid BundleId { get; set; }
    public string BundleTitle { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "PKR";
    public PaymentGateway Gateway { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? ExternalPaymentId { get; set; }
    public string? ExternalSessionId { get; set; }
    public string? FailureReason { get; set; }
    public string? FailureCode { get; set; }
    public DateTime? PaidAt { get; set; }
    public Guid? EnrollmentId { get; set; }
    public string? StudentCountry { get; set; }
    public string? MetadataJson { get; set; }
}

public sealed class PaymentWebhookEvent : BaseEntity
{
    public PaymentGateway Gateway { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string ExternalEventId { get; set; } = string.Empty;
    public Guid? PaymentOrderId { get; set; }
    public Guid? TenantId { get; set; }
    public string? RawPayload { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}
