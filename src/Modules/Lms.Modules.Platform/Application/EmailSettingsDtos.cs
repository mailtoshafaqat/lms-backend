namespace Lms.Modules.Platform.Application;

/// <summary>Returned to the admin UI. The SMTP password is never returned; <see cref="HasPassword"/>
/// signals whether one is stored.</summary>
public sealed record EmailSettingsDto(
    bool Enabled,
    string FromEmail,
    string FromName,
    string SmtpHost,
    int SmtpPort,
    string SmtpUser,
    bool HasPassword,
    bool UseSsl);

/// <summary>Update payload. When <see cref="SmtpPassword"/> is null/empty the existing password is kept.</summary>
public sealed record UpdateEmailSettingsRequest(
    bool Enabled,
    string FromEmail,
    string FromName,
    string SmtpHost,
    int SmtpPort,
    string SmtpUser,
    string? SmtpPassword,
    bool UseSsl);

/// <summary>Returned to the admin UI. The Zoom client secret is never returned;
/// <see cref="HasClientSecret"/> signals whether one is stored.</summary>
public sealed record ZoomSettingsDto(
    bool Enabled,
    string AccountId,
    string ClientId,
    bool HasClientSecret);

/// <summary>Update payload. When <see cref="ClientSecret"/> is null/empty the existing secret is kept.</summary>
public sealed record UpdateZoomSettingsRequest(
    bool Enabled,
    string AccountId,
    string ClientId,
    string? ClientSecret);
