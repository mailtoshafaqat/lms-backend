using Lms.Shared.Auth;
using Lms.Shared.Enrollments;

namespace Lms.Modules.Enrollment.Application;

public sealed class EnrollmentAccessGuard : IEnrollmentAccessGuard
{
    private readonly IEnrollmentReader _enrollments;

    public EnrollmentAccessGuard(IEnrollmentReader enrollments) => _enrollments = enrollments;

    public async Task<bool> HasBundleAccessAsync(
        Guid? userId, string? role, Guid bundleId, CancellationToken ct = default)
    {
        if (role is Roles.SuperAdmin or Roles.InstituteAdmin or Roles.Teacher)
            return true;

        if (userId is null)
            return false;

        var bundles = await _enrollments.GetActiveBundleIdsAsync(userId.Value, ct);
        return bundles.Contains(bundleId);
    }
}
