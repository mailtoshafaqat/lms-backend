using Lms.Modules.Progress.Domain;

namespace Lms.Modules.Progress.Application;

public interface ICertificatePdfService
{
    Task<byte[]> RenderAsync(
        CompletionCertificate certificate,
        CertificateTemplate template,
        string tenantSlug,
        CancellationToken ct = default);
}
