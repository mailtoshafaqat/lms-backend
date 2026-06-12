using Lms.Shared.Entities;

namespace Lms.Modules.Progress.Domain;

/// <summary>Issued when a student completes all topics in an enrolled bundle.</summary>
public sealed class CompletionCertificate : TenantEntity
{
    public Guid UserId { get; set; }
    public Guid BundleId { get; set; }
    public string BundleTitle { get; set; } = string.Empty;
    public string CertificateNumber { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Snapshot at issue time — survives template edits.</summary>
    public string StudentName { get; set; } = string.Empty;
    public string InstituteName { get; set; } = string.Empty;
    public int TemplateVersion { get; set; } = 1;
}
