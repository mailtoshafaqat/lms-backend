using Lms.Modules.Courses.Infrastructure;
using Lms.Shared.Courses;
using Lms.Shared.Enrollments;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Courses.Application;

public sealed class SubjectCatalogReader : ISubjectCatalogReader
{
    private readonly CoursesDbContext _db;
    private readonly IEnrollmentReader _enrollments;

    public SubjectCatalogReader(CoursesDbContext db, IEnrollmentReader enrollments)
    {
        _db = db;
        _enrollments = enrollments;
    }

    public async Task<IReadOnlyList<Guid>> GetEnrolledStudentIdsForDefinitionAsync(
        Guid subjectDefinitionId, CancellationToken ct = default)
    {
        var bundleIds = await _db.Subjects.AsNoTracking()
            .Where(s => s.SubjectDefinitionId == subjectDefinitionId)
            .Select(s => s.BundleId)
            .Distinct()
            .ToListAsync(ct);

        if (bundleIds.Count == 0) return [];

        var userIds = new HashSet<Guid>();
        foreach (var bundleId in bundleIds)
        {
            var ids = await _enrollments.GetActiveUserIdsForBundleAsync(bundleId, ct);
            foreach (var id in ids) userIds.Add(id);
        }

        return userIds.ToList();
    }
}
