namespace Lms.Shared.Notifications;

public sealed record CreateStudentNotificationRequest(
    Guid TenantId,
    Guid UserId,
    string Title,
    string Body,
    string? LinkUrl = null,
    bool SendEmail = true,
    string? EmailSubject = null);

/// <summary>Cross-module contract for in-app student notifications (and optional email).</summary>
public interface IStudentNotificationService
{
    Task NotifyAsync(CreateStudentNotificationRequest request, CancellationToken ct = default);

    Task NotifyManyAsync(
        IReadOnlyList<CreateStudentNotificationRequest> requests,
        CancellationToken ct = default);
}
