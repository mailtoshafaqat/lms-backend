namespace Lms.Modules.Progress.Application;

public sealed record CertificateTemplateDto(
    string Title,
    string Subtitle,
    string? BackgroundUrl,
    string? LogoUrl,
    string? SignatureUrl,
    string SignatureLabel,
    string PrimaryColor,
    bool ShowQrCode,
    bool Enabled,
    int Version);

public sealed record UpdateCertificateTemplateRequest(
    string Title,
    string Subtitle,
    string? BackgroundUrl,
    string? LogoUrl,
    string? SignatureUrl,
    string SignatureLabel,
    string PrimaryColor,
    bool ShowQrCode,
    bool Enabled);

public sealed record CertificateVerifyDto(
    bool Valid,
    string CertificateNumber,
    string StudentName,
    string CourseName,
    string InstituteName,
    DateTime IssuedAt,
    string? TenantSlug);
