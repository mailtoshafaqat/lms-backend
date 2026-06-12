using Lms.Shared.Common;

namespace Lms.Modules.Progress.Application;

public interface ICertificateService
{
    Task TryIssueIfCompleteAsync(Guid userId, Guid bundleId, CancellationToken ct = default);

    Task<IReadOnlyList<CertificateDto>> ListMineAsync(Guid userId, CancellationToken ct = default);

    Task<PagedResult<AdminCertificateDto>> ListAdminAsync(
        Guid? bundleId, int page, int pageSize, CancellationToken ct = default);

    Task<byte[]?> GetPdfForStudentAsync(Guid certificateId, Guid userId, CancellationToken ct = default);

    Task<byte[]?> GetPdfForAdminAsync(Guid certificateId, CancellationToken ct = default);

    Task<CertificateVerifyDto?> VerifyAsync(string certificateNumber, string tenantSlug, CancellationToken ct = default);
}
