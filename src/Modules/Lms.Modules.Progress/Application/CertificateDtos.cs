namespace Lms.Modules.Progress.Application;

public sealed record CertificateDto(
    Guid Id,
    Guid BundleId,
    string BundleTitle,
    string CertificateNumber,
    DateTime IssuedAt,
    string? VerifyUrl);

public sealed record AdminCertificateDto(
    Guid Id,
    Guid UserId,
    string StudentName,
    Guid BundleId,
    string BundleTitle,
    string CertificateNumber,
    DateTime IssuedAt);
