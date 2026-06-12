using Lms.Modules.Courses.Infrastructure;
using Lms.Shared.Courses;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Courses.Application;

public sealed class CourseContentSearch : ICourseContentSearch
{
    private readonly CoursesDbContext _db;

    public CourseContentSearch(CoursesDbContext db) => _db = db;

    public async Task<IReadOnlyList<ContentSearchHitDto>> SearchAsync(
        string query,
        IReadOnlyList<Guid>? limitToBundleIds,
        int take = 20,
        CancellationToken ct = default)
    {
        var term = query.Trim();
        if (term.Length < 2) return [];

        var size = Math.Clamp(take, 1, 50);
        var pattern = $"%{term}%";
        var bundleFilter = limitToBundleIds is { Count: > 0 } ? limitToBundleIds : null;

        var topicHits = await (
            from t in _db.Topics.AsNoTracking()
            join u in _db.Units.AsNoTracking() on t.UnitId equals u.Id
            join s in _db.Subjects.AsNoTracking() on u.SubjectId equals s.Id
            join b in _db.Bundles.AsNoTracking() on s.BundleId equals b.Id
            where b.IsPublished
                  && EF.Functions.Like(t.Title, pattern)
                  && (bundleFilter == null || bundleFilter.Contains(b.Id))
            orderby t.Title
            select new ContentSearchHitDto(
                "Topic",
                t.Id,
                t.Title,
                b.Title + " / " + s.Title + " / " + u.Title,
                t.Id,
                s.Id,
                b.Id))
            .Take(size)
            .ToListAsync(ct);

        if (topicHits.Count >= size) return topicHits;

        var remaining = size - topicHits.Count;
        var subjectHits = await (
            from s in _db.Subjects.AsNoTracking()
            join b in _db.Bundles.AsNoTracking() on s.BundleId equals b.Id
            where b.IsPublished
                  && EF.Functions.Like(s.Title, pattern)
                  && (bundleFilter == null || bundleFilter.Contains(b.Id))
            orderby s.Title
            select new ContentSearchHitDto(
                "Subject",
                s.Id,
                s.Title,
                b.Title,
                null,
                s.Id,
                b.Id))
            .Take(remaining)
            .ToListAsync(ct);

        return topicHits.Concat(subjectHits).ToList();
    }
}
