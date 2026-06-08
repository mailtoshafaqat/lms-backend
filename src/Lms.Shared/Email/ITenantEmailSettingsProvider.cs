namespace Lms.Shared.Email;

/// <summary>Per-tenant SMTP / sender configuration (white-label: each tenant sends from its own address).</summary>
public sealed record TenantEmailSettings(
    bool Enabled,
    string FromEmail,
    string FromName,
    string SmtpHost,
    int SmtpPort,
    string SmtpUser,
    string? SmtpPassword,
    bool UseSsl);

public interface ITenantEmailSettingsProvider
{
    Task<TenantEmailSettings?> GetAsync(CancellationToken ct = default);
}
