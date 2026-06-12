namespace Lms.Modules.Progress.Application;

public interface ICertificateTemplateService
{
    Task<CertificateTemplateDto> GetAsync(CancellationToken ct = default);
    Task<CertificateTemplateDto> SaveAsync(UpdateCertificateTemplateRequest request, CancellationToken ct = default);
    Task<Lms.Modules.Progress.Domain.CertificateTemplate> GetOrCreateEntityAsync(CancellationToken ct = default);
}
