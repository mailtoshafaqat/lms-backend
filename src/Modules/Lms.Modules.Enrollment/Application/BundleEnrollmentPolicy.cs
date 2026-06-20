using Lms.Modules.Courses.Contracts;
using Lms.Shared.Enrollments;

namespace Lms.Modules.Enrollment.Application;

public sealed class BundleEnrollmentPolicy : IBundleEnrollmentPolicy
{
    private readonly IBundleCatalog _catalog;
    private readonly IEnrollmentReader _enrollments;

    public BundleEnrollmentPolicy(IBundleCatalog catalog, IEnrollmentReader enrollments)
    {
        _catalog = catalog;
        _enrollments = enrollments;
    }

    public async Task<BundleEnrollmentPolicyResult> CheckCanEnrollAsync(
        Guid bundleId, BundleEnrollmentCheckMode mode, CancellationToken ct = default)
    {
        var bundle = await _catalog.GetBundleAsync(bundleId, ct);
        if (bundle is null || !bundle.IsPublished)
            return new BundleEnrollmentPolicyResult(false, "Bundle not found.", 0, null);

        var active = (await _enrollments.GetActiveUserIdsForBundleAsync(bundleId, ct)).Count;
        if (mode == BundleEnrollmentCheckMode.AdminOverride)
            return new BundleEnrollmentPolicyResult(true, null, active, bundle.MaxEnrollments);

        var now = DateTime.UtcNow;
        if (bundle.EnrollmentOpensAt is not null && now < bundle.EnrollmentOpensAt.Value)
        {
            return new BundleEnrollmentPolicyResult(
                false,
                $"Enrollment opens on {bundle.EnrollmentOpensAt.Value:u}.",
                active,
                bundle.MaxEnrollments);
        }

        if (bundle.EnrollmentClosesAt is not null && now > bundle.EnrollmentClosesAt.Value)
        {
            return new BundleEnrollmentPolicyResult(
                false,
                $"Enrollment closed on {bundle.EnrollmentClosesAt.Value:u}.",
                active,
                bundle.MaxEnrollments);
        }

        if (bundle.EndsAt is not null && now > bundle.EndsAt.Value)
        {
            return new BundleEnrollmentPolicyResult(
                false,
                "This batch has ended.",
                active,
                bundle.MaxEnrollments);
        }

        if (bundle.MaxEnrollments is > 0 && active >= bundle.MaxEnrollments.Value)
        {
            return new BundleEnrollmentPolicyResult(
                false,
                $"Batch is full ({active} of {bundle.MaxEnrollments.Value} seats).",
                active,
                bundle.MaxEnrollments);
        }

        return new BundleEnrollmentPolicyResult(true, null, active, bundle.MaxEnrollments);
    }

    public async Task<bool> IsContentAccessibleAsync(Guid bundleId, CancellationToken ct = default)
    {
        var bundle = await _catalog.GetBundleAsync(bundleId, ct);
        if (bundle is null)
            return false;
        if (bundle.StartsAt is null)
            return true;
        return DateTime.UtcNow >= bundle.StartsAt.Value;
    }
}
