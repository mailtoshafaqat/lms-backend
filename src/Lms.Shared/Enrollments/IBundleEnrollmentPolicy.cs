namespace Lms.Shared.Enrollments;

public enum BundleEnrollmentCheckMode
{
    /// <summary>Self-enroll, checkout, and payment webhooks — enforces cap and enrollment window.</summary>
    Student = 0,

    /// <summary>Admin create/enroll — bypasses cap and enrollment window; batch must still exist.</summary>
    AdminOverride = 1
}

public sealed record BundleEnrollmentPolicyResult(
    bool Allowed,
    string? ErrorMessage,
    int ActiveEnrollments,
    int? MaxEnrollments);

public interface IBundleEnrollmentPolicy
{
    Task<BundleEnrollmentPolicyResult> CheckCanEnrollAsync(
        Guid bundleId, BundleEnrollmentCheckMode mode, CancellationToken ct = default);

    /// <summary>Whether enrolled students may access bundle content (respects batch start date).</summary>
    Task<bool> IsContentAccessibleAsync(Guid bundleId, CancellationToken ct = default);
}
