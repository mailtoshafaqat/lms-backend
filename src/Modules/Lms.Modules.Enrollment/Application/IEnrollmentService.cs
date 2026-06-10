using Lms.Shared.Common;

namespace Lms.Modules.Enrollment.Application;

public interface IEnrollmentService
{
    Task<Result<EnrollmentDto>> EnrollAsync(Guid userId, Guid bundleId, CancellationToken ct = default);

    /// <summary>Admin-provisioned enrollment — bypasses self-enroll tenant flag.</summary>
    Task<Result<EnrollmentDto>> ProvisionEnrollmentAsync(Guid userId, Guid bundleId, CancellationToken ct = default);

    Task<IReadOnlyList<EnrollmentDto>> GetMyEnrollmentsAsync(Guid userId, CancellationToken ct = default);
}
