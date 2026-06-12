using Lms.Modules.Courses.Infrastructure;
using Lms.Shared.Courses;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Courses.Application;

public sealed class CourseTopicCatalog : ICourseTopicCatalog
{
    private readonly CoursesDbContext _db;

    public CourseTopicCatalog(CoursesDbContext db) => _db = db;

    public async Task<IReadOnlyList<TopicPathDto>> GetTopicPathsForBundlesAsync(
        IReadOnlyList<Guid> bundleIds,
        CancellationToken ct = default)
    {
        if (bundleIds.Count == 0) return [];

        var ownUnitPaths = await (
            from t in _db.Topics.AsNoTracking()
            join u in _db.Units.AsNoTracking() on t.UnitId equals u.Id
            join s in _db.Subjects.AsNoTracking() on u.SubjectId equals s.Id
            join b in _db.Bundles.AsNoTracking() on s.BundleId equals b.Id
            where bundleIds.Contains(b.Id)
            select new TopicPathDto(
                t.Id,
                t.Title,
                s.Id,
                s.Title,
                b.Id,
                b.Title))
            .ToListAsync(ct);

        var sharedUnitPaths = await (
            from t in _db.Topics.AsNoTracking()
            join u in _db.Units.AsNoTracking() on t.UnitId equals u.Id
            join link in _db.SubjectSharedUnits.AsNoTracking() on u.Id equals link.UnitId
            join s in _db.Subjects.AsNoTracking() on link.SubjectId equals s.Id
            join b in _db.Bundles.AsNoTracking() on s.BundleId equals b.Id
            where bundleIds.Contains(b.Id)
            select new TopicPathDto(
                t.Id,
                t.Title,
                s.Id,
                s.Title,
                b.Id,
                b.Title))
            .ToListAsync(ct);

        return ownUnitPaths
            .Concat(sharedUnitPaths)
            .GroupBy(p => p.TopicId)
            .Select(g => g.First())
            .OrderBy(p => p.BundleTitle)
            .ThenBy(p => p.SubjectTitle)
            .ThenBy(p => p.TopicTitle)
            .ToList();
    }
}
