namespace Lms.Shared.Enrollments;

/// <summary>Checks whether a user may access bundle-scoped student content (lectures, quizzes, files).</summary>
public interface IEnrollmentAccessGuard
{
    /// <summary>
    /// Institute admins, super admins, and teachers always pass.
    /// Students must have an active enrollment in <paramref name="bundleId"/>.
    /// </summary>
    Task<bool> HasBundleAccessAsync(
        Guid? userId, string? role, Guid bundleId, CancellationToken ct = default);
}
