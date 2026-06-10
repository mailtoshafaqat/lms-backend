using Lms.Modules.Courses.Infrastructure;
using Lms.Shared.Courses;
using Lms.Shared.Enrollments;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Courses.Application;

/// <summary>Adapts enrollment + course catalog to the shared enrolled-subjects contract.</summary>
public sealed class EnrolledSubjectsReader : IEnrolledSubjectsReader
{
    private readonly IEnrollmentReader _enrollments;
    private readonly CoursesDbContext _db;

    public EnrolledSubjectsReader(IEnrollmentReader enrollments, CoursesDbContext db)
    {
        _enrollments = enrollments;
        _db = db;
    }

    public async Task<IReadOnlyList<AssignedSubjectDto>> GetEnrolledSubjectsAsync(
        Guid userId, CancellationToken ct = default)
    {
        var bundleIds = await _enrollments.GetActiveBundleIdsAsync(userId, ct);
        if (bundleIds.Count == 0) return [];

        return await _db.Subjects.AsNoTracking()
            .Where(s => bundleIds.Contains(s.BundleId))
            .OrderBy(s => s.Order)
            .Select(s => new AssignedSubjectDto(s.Id, s.Title, s.BundleId, s.Bundle!.Title))
            .ToListAsync(ct);
    }
}
