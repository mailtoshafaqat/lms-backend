namespace Lms.Shared.Enrollments;

public sealed record EnrollmentSummary(Guid BundleId, string BundleTitle, DateTime ExpiresAt);

/// <summary>Cross-module write contract for granting course access. Implemented by the Enrollment
/// module; used (e.g.) by Identity when an admin provisions a student into a course.</summary>
public interface IEnrollmentWriter
{
    Task<EnrollmentSummary?> EnrollAsync(Guid userId, Guid bundleId, CancellationToken ct = default);
}

/// <summary>Cross-module read contract for a user's current course access. Implemented by the
/// Enrollment module; used (e.g.) by LiveClasses to show only the classes a student may join.</summary>
public interface IEnrollmentReader
{
    Task<IReadOnlyList<Guid>> GetActiveBundleIdsAsync(Guid userId, CancellationToken ct = default);
}
