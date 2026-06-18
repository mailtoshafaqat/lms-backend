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
            .Select(b => new BundleDto(b.Id, b.Title, b.Subjects.Count, b.Price, b.VideosOnly))
            .ToListAsync(ct);

    public async Task<BundleDetailDto?> GetBundleAsync(Guid bundleId, CancellationToken ct = default)
    {
        var subjects = await _db.Subjects
            .Where(s => s.BundleId == bundleId)
            .OrderBy(s => s.Order)
            .Select(s => new
            {
                s.Id,
                s.Title,
                s.Order,
                s.SubjectDefinitionId,
                OwnUnitCount = s.Units.Count,
                SharedUnitCount = s.SharedUnitLinks.Count
            })
            .ToListAsync(ct);

        if (subjects.Count == 0 && !await _db.Bundles.AnyAsync(b => b.Id == bundleId, ct))
            return null;

        var bundleTitle = await _db.Bundles
            .Where(b => b.Id == bundleId)
            .Select(b => b.Title)
            .FirstAsync(ct);

        return new BundleDetailDto(
            bundleId,
            bundleTitle,
            subjects.Select(s => new SubjectDto(
                s.Id,
                s.Title,
                s.Order,
                s.OwnUnitCount + s.SharedUnitCount,
                s.SubjectDefinitionId,
                s.SubjectDefinitionId is not null,
                s.SharedUnitCount)).ToList());
    }

    public async Task<IReadOnlyList<UnitDto>> GetUnitsAsync(Guid subjectId, CancellationToken ct = default)
    {
        var ownUnits = await _db.Units
            .Where(u => u.SubjectId == subjectId)
            .OrderBy(u => u.Order)
            .Select(u => new UnitDto(u.Id, u.Title, u.Order, u.Topics.Count, false))
            .ToListAsync(ct);

        var sharedUnits = await _db.SubjectSharedUnits
            .Where(l => l.SubjectId == subjectId)
            .OrderBy(l => l.Order)
            .Select(l => new UnitDto(
                l.Unit!.Id,
                l.Unit.Title,
                l.Order,
                l.Unit.Topics.Count,
                true))
            .ToListAsync(ct);

        return ownUnits.Concat(sharedUnits).ToList();
    }

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
