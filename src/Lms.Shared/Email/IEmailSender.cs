namespace Lms.Shared.Email;

public sealed record EmailMessage(string ToEmail, string ToName, string Subject, string HtmlBody);

/// <summary>Cross-cutting email sender. Implemented by the Platform module (per-tenant SMTP
/// with a dev-outbox fallback) so any module can send mail without owning SMTP details.</summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken ct = default);

    /// <summary>Sends using a specific tenant's SMTP (e.g. password reset before the user is authenticated).</summary>
    Task SendForTenantAsync(Guid tenantId, EmailMessage message, CancellationToken ct = default);
}
