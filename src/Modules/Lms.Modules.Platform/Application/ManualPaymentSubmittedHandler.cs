using Lms.Shared.Branding;
using Lms.Shared.Email;
using Lms.Shared.Events;
using Lms.Shared.Payments;
using Lms.Shared.Users;
using Microsoft.Extensions.Logging;

namespace Lms.Modules.Platform.Application;

public sealed class ManualPaymentSubmittedHandler : IEventHandler<ManualPaymentSubmittedEvent>
{
    private readonly IEmailSender _email;
    private readonly IBrandedEmailRenderer _branded;
    private readonly IInstituteAdminReader _admins;
    private readonly ILogger<ManualPaymentSubmittedHandler> _logger;

    public ManualPaymentSubmittedHandler(
        IEmailSender email,
        IBrandedEmailRenderer branded,
        IInstituteAdminReader admins,
        ILogger<ManualPaymentSubmittedHandler> logger)
    {
        _email = email;
        _branded = branded;
        _admins = admins;
        _logger = logger;
    }

    public async Task HandleAsync(ManualPaymentSubmittedEvent @event, CancellationToken cancellationToken = default)
    {
        var admins = await _admins.ListByTenantAsync(@event.TenantId, cancellationToken);
        var recipients = admins.Where(a => a.IsActive).Select(a => a.Email).Distinct().ToList();
        if (recipients.Count == 0)
        {
            _logger.LogWarning(
                "Manual payment {OrderId}: no institute admin emails to notify for tenant {TenantId}.",
                @event.OrderId, @event.TenantId);
            return;
        }

        var noteLine = string.IsNullOrWhiteSpace(@event.Note)
            ? ""
            : $"<p><strong>Note:</strong> {System.Net.WebUtility.HtmlEncode(@event.Note)}</p>";

        var body =
            $"<p>A student submitted a manual payment that needs your review.</p>" +
            $"<ul>" +
            $"<li><strong>Student:</strong> {System.Net.WebUtility.HtmlEncode(@event.StudentName)} ({System.Net.WebUtility.HtmlEncode(@event.StudentEmail)})</li>" +
            $"<li><strong>Course:</strong> {System.Net.WebUtility.HtmlEncode(@event.BundleTitle)}</li>" +
            $"<li><strong>Amount:</strong> {@event.Currency} {@event.Amount:N0}</li>" +
            $"<li><strong>Transaction ref:</strong> {System.Net.WebUtility.HtmlEncode(@event.TransactionRef)}</li>" +
            $"</ul>" +
            noteLine +
            $"<p>Review and approve in <strong>Admin → Payments</strong>.</p>";

        var html = await _branded.RenderAsync(@event.TenantId, "Manual payment awaiting review", body, cancellationToken);

        foreach (var email in recipients)
        {
            try
            {
                await _email.SendForTenantAsync(
                    @event.TenantId,
                    new EmailMessage(email, null, "Manual payment awaiting review", html),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to notify {Email} about manual payment {OrderId}", email, @event.OrderId);
            }
        }
    }
}
