using Lms.Modules.Courses.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Courses.Application;

public sealed class CourseService : ICourseService
{
    private readonly CoursesDbContext _db;

    public CourseService(CoursesDbContext db) => _db = db;

    public async Task<IReadOnlyList<BundleDto>> GetBundlesAsync(CancellationToken ct = default) =>
        await _db.Bundles
            .Where(b => b.IsPublished)
            .OrderBy(b => b.Title)
            .Select(b => new BundleDto(b.Id, b.Title, b.Subjects.Count, b.Price))
            .ToListAsync(ct);

    public async Task<BundleDetailDto?> GetBundleAsync(Guid bundleId, CancellationToken ct = default)
    {
        var bundle = await _db.Bundles
            .Where(b => b.Id == bundleId)
            .Select(b => new BundleDetailDto(
                b.Id,
                b.Title,
                b.Subjects
                    .OrderBy(s => s.Order)
                    .Select(s => new SubjectDto(s.Id, s.Title, s.Order, s.Units.Count))
                    .ToList()))
            .FirstOrDefaultAsync(ct);

        return bundle;
    }

    public async Task<IReadOnlyList<UnitDto>> GetUnitsAsync(Guid subjectId, CancellationToken ct = default) =>
        await _db.Units
            .Where(u => u.SubjectId == subjectId)
            .OrderBy(u => u.Order)
            .Select(u => new UnitDto(u.Id, u.Title, u.Order, u.Topics.Count))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<TopicDto>> GetTopicsAsync(Guid unitId, CancellationToken ct = default) =>
        await _db.Topics
            .Where(t => t.UnitId == unitId)
            .OrderBy(t => t.Order)
            .Select(t => new TopicDto(t.Id, t.Title, t.Order, t.HasVideo, t.McqCount, t.FlashcardCount))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<TopicDto>> GetRecentTopicsAsync(int take, CancellationToken ct = default) =>
        await _db.Topics
            .OrderBy(t => t.Order)
            .Take(take)
            .Select(t => new TopicDto(t.Id, t.Title, t.Order, t.HasVideo, t.McqCount, t.FlashcardCount))
            .ToListAsync(ct);
}
