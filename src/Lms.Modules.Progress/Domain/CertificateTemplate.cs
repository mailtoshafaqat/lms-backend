using Lms.Shared.Entities;

namespace Lms.Modules.Progress.Domain;

/// <summary>Per-tenant completion certificate layout (Phase A: one template per institute).</summary>
public sealed class CertificateTemplate : TenantEntity
{
    public string Title { get; set; } = "Certificate of Completion";
    public string Subtitle { get; set; } = "This is to certify that";
    public string? BackgroundUrl { get; set; }
    public string? LogoUrl { get; set; }
    public string? SignatureUrl { get; set; }
    public string SignatureLabel { get; set; } = "Authorized signatory";
    public string PrimaryColor { get; set; } = "#0b3d91";
    public bool ShowQrCode { get; set; } = true;
    public bool Enabled { get; set; } = true;
    public int Version { get; set; } = 1;
}
