namespace Lms.Modules.Courses.Contracts;

/// <summary>Read-only summary of a bundle for other modules (e.g. Enrollment) — a well-defined
/// interface so they don't reach into Courses internals or duplicate its data.</summary>
public sealed record BundleSummary(Guid Id, string Title, decimal Price, int ValidityDays, bool IsPublished);

public interface IBundleCatalog
{
    Task<BundleSummary?> GetBundleAsync(Guid bundleId, CancellationToken ct = default);
}
