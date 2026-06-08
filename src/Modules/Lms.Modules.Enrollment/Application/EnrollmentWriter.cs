using Lms.Modules.Enrollment.Infrastructure;
using Lms.Shared.Enrollments;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Enrollment.Application;

/// <summary>Adapts the internal enrollment service to the shared cross-module write contract.</summary>
public sealed class EnrollmentWriter : IEnrollmentWriter
{
    private readonly IEnrollmentService _enrollments;

    public EnrollmentWriter(IEnrollmentService enrollments) => _enrollments = enrollments;

    public async Task<EnrollmentSummary?> EnrollAsync(Guid userId, Guid bundleId, CancellationToken ct = default)
    {
        var result = await _enrollments.EnrollAsync(userId, bundleId, ct);
        if (!result.Succeeded || result.Value is null) return null;

        var e = result.Value;
        return new EnrollmentSummary(e.BundleId, e.BundleTitle, e.ExpiresAt);
    }
}

/// <summary>Adapts enrollment storage to the shared cross-module read contract.</summary>
public sealed class EnrollmentReader : IEnrollmentReader
{
    private readonly EnrollmentDbContext _db;

    public EnrollmentReader(EnrollmentDbContext db) => _db = db;

    public async Task<IReadOnlyList<Guid>> GetActiveBundleIdsAsync(Guid userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await _db.Enrollments
            .Where(e => e.UserId == userId && e.ExpiresAt > now)
            .Select(e => e.BundleId)
            .Distinct()
            .ToListAsync(ct);
    }
}
