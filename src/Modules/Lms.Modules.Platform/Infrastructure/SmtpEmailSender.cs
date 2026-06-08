using Lms.Modules.Platform.Domain;
using Lms.Shared.Email;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Lms.Modules.Platform.Infrastructure;

/// <summary>Sends mail using the current tenant's SMTP settings. When the tenant has not configured
/// (or disabled) email, it falls back to a dev outbox: the message is written to disk and logged,
/// so flows that send email keep working before real SMTP credentials are entered.</summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly ITenantEmailSettingsProvider _settings;
    private readonly PlatformDbContext _db;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(
        ITenantEmailSettingsProvider settings, PlatformDbContext db, ILogger<SmtpEmailSender> logger)
    {
        _settings = settings;
        _db = db;
        _logger = logger;
    }

    public Task SendAsync(EmailMessage message, CancellationToken ct = default) =>
        SendWithSettingsAsync(message, _settings.GetAsync(ct), ct);

    public Task SendForTenantAsync(Guid tenantId, EmailMessage message, CancellationToken ct = default) =>
        SendWithSettingsAsync(message, LoadSettingsForTenantAsync(tenantId, ct), ct);

    private async Task SendWithSettingsAsync(
        EmailMessage message, Task<TenantEmailSettings?> settingsTask, CancellationToken ct)
    {
        var settings = await settingsTask;

        if (settings is null || !settings.Enabled || string.IsNullOrWhiteSpace(settings.SmtpHost))
        {
            await WriteToDevOutboxAsync(message, settings?.FromEmail, ct);
            return;
        }

        await SendMimeAsync(message, settings, ct);
        _logger.LogInformation("Email sent to {To} via {Host}", message.ToEmail, settings.SmtpHost);
    }

    private async Task<TenantEmailSettings?> LoadSettingsForTenantAsync(Guid tenantId, CancellationToken ct)
    {
        var s = await _db.TenantSettings.IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, ct);
        if (s is null) return null;

        return new TenantEmailSettings(
            s.EmailEnabled, s.FromEmail, s.FromName,
            s.SmtpHost, s.SmtpPort, s.SmtpUser, s.SmtpPassword, s.UseSsl);
    }

    private static async Task SendMimeAsync(EmailMessage message, TenantEmailSettings settings, CancellationToken ct)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(settings.FromName, settings.FromEmail));
        mime.To.Add(new MailboxAddress(message.ToName, message.ToEmail));
        mime.Subject = message.Subject;
        mime.Body = new BodyBuilder { HtmlBody = message.HtmlBody }.ToMessageBody();

        using var client = new SmtpClient();
        var socketOptions = settings.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
        await client.ConnectAsync(settings.SmtpHost, settings.SmtpPort, socketOptions, ct);
        if (!string.IsNullOrWhiteSpace(settings.SmtpUser))
            await client.AuthenticateAsync(settings.SmtpUser, settings.SmtpPassword ?? string.Empty, ct);
        await client.SendAsync(mime, ct);
        await client.DisconnectAsync(true, ct);
    }

    private async Task WriteToDevOutboxAsync(EmailMessage message, string? from, CancellationToken ct)
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "dev-emails");
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, $"{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}-{message.ToEmail}.html");
        var contents =
            $"<!-- FROM: {from ?? "(not configured)"} TO: {message.ToEmail} SUBJECT: {message.Subject} -->\n{message.HtmlBody}";
        await File.WriteAllTextAsync(file, contents, ct);
        _logger.LogWarning(
            "Email NOT sent (tenant SMTP not configured). Written to dev outbox: {File}", file);
    }
}
