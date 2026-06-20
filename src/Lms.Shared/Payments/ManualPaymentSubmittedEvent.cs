using Lms.Shared.Events;

namespace Lms.Shared.Payments;

/// <summary>Published when a student submits a manual payment awaiting admin review.</summary>
public sealed record ManualPaymentSubmittedEvent(
    Guid OrderId,
    Guid TenantId,
    Guid UserId,
    string StudentName,
    string StudentEmail,
    Guid BundleId,
    string BundleTitle,
    decimal Amount,
    string Currency,
    string TransactionRef,
    string? Note) : IEvent;
