using Lms.Modules.Courses.Infrastructure;
using Lms.Shared.Courses;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Courses.Application;

public sealed class CourseScopeReader : ICourseScopeReader
{
    private readonly CoursesDbContext _db;

    public CourseScopeReader(CoursesDbContext db) => _db = db;

    public async Task<TopicScope?> GetTopicScopeAsync(Guid topicId, CancellationToken ct = default)
    {
        var row = await _db.Topics.AsNoTracking()
            .Where(t => t.Id == topicId)
            .Select(t => new
            {
                t.Id,
                t.Title,
                UnitId = t.UnitId,
                SubjectId = t.Unit!.SubjectId,
                SubjectTitle = t.Unit.Subject != null ? t.Unit.Subject.Title : null,
                BundleId = t.Unit.Subject != null ? (Guid?)t.Unit.Subject.BundleId : null
            })
            .FirstOrDefaultAsync(ct);

        if (row is null) return null;

        if (row.SubjectId is Guid subjectId && row.BundleId is Guid bundleId)
        {
            return new TopicScope(
                row.Id,
                row.Title,
                subjectId,
                row.SubjectTitle ?? string.Empty,
                bundleId);
        }

        var link = await _db.SubjectSharedUnits.AsNoTracking()
            .Where(l => l.UnitId == row.UnitId)
            .Select(l => new { l.SubjectId, l.Subject!.Title, l.Subject.BundleId })
            .FirstOrDefaultAsync(ct);

        return link is null
            ? null
            : new TopicScope(row.Id, row.Title, link.SubjectId, link.Title, link.BundleId);
    }

    public async Task<SubjectScope?> GetSubjectScopeAsync(Guid subjectId, CancellationToken ct = default) =>
        await _db.Subjects.AsNoTracking()
            .Where(s => s.Id == subjectId)
            .Select(s => new SubjectScope(s.Id, s.Title, s.BundleId, s.Bundle!.Title))
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Guid>> GetTopicIdsForSubjectAsync(Guid subjectId, CancellationToken ct = default)
    {
        var own = await _db.Topics.AsNoTracking()
            .Where(t => t.Unit!.SubjectId == subjectId)
            .Select(t => t.Id)
            .ToListAsync(ct);

        var sharedUnitIds = await _db.SubjectSharedUnits.AsNoTracking()
            .Where(l => l.SubjectId == subjectId)
            .Select(l => l.UnitId)
            .ToListAsync(ct);

        if (sharedUnitIds.Count == 0) return own;

        var sharedTopics = await _db.Topics.AsNoTracking()
            .Where(t => sharedUnitIds.Contains(t.UnitId))
            .Select(t => t.Id)
            .ToListAsync(ct);

        return own.Concat(sharedTopics).Distinct().ToList();
    }

    public async Task<IReadOnlyList<Guid>> GetTopicIdsForUnitAsync(Guid unitId, CancellationToken ct = default) =>
        await _db.Topics.AsNoTracking()
            .Where(t => t.UnitId == unitId)
            .Select(t => t.Id)
            .ToListAsync(ct);

    public async Task<IReadOnlyDictionary<Guid, TopicScope>> GetTopicScopesAsync(
        IReadOnlyList<Guid> topicIds, CancellationToken ct = default)
    {
        if (topicIds.Count == 0) return new Dictionary<Guid, TopicScope>();

        var idSet = topicIds.Distinct().ToList();
        var rows = await _db.Topics.AsNoTracking()
            .Where(t => idSet.Contains(t.Id))
            .Select(t => new
            {
                t.Id,
                t.Title,
                t.UnitId,
                SubjectId = t.Unit!.SubjectId,
                SubjectTitle = t.Unit.Subject != null ? t.Unit.Subject.Title : null,
                BundleId = t.Unit.Subject != null ? (Guid?)t.Unit.Subject.BundleId : null
            })
            .ToListAsync(ct);

        var map = new Dictionary<Guid, TopicScope>();
        var needsShared = new List<(Guid TopicId, string Title, Guid UnitId)>();

        foreach (var row in rows)
        {
            if (row.SubjectId is Guid subjectId && row.BundleId is Guid bundleId)
            {
                map[row.Id] = new TopicScope(
                    row.Id, row.Title, subjectId, row.SubjectTitle ?? string.Empty, bundleId);
            }
            else
            {
                needsShared.Add((row.Id, row.Title, row.UnitId));
            }
        }

        if (needsShared.Count > 0)
        {
            var unitIds = needsShared.Select(x => x.UnitId).Distinct().ToList();
            var links = await _db.SubjectSharedUnits.AsNoTracking()
                .Where(l => unitIds.Contains(l.UnitId))
                .Select(l => new { l.UnitId, l.SubjectId, l.Subject!.Title, l.Subject.BundleId })
                .ToListAsync(ct);

            var linkByUnit = links.GroupBy(l => l.UnitId).ToDictionary(g => g.Key, g => g.First());
            foreach (var (topicId, title, unitId) in needsShared)
            {
                if (linkByUnit.TryGetValue(unitId, out var link))
                {
                    map[topicId] = new TopicScope(
                        topicId, title, link.SubjectId, link.Title, link.BundleId);
                }
            }
        }

        return map;
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetTopicCountsByBundleAsync(
        IReadOnlyList<Guid> bundleIds, CancellationToken ct = default)
    {
        if (bundleIds.Count == 0) return new Dictionary<Guid, int>();

        var topicIdsByBundle = bundleIds.ToDictionary(id => id, _ => new HashSet<Guid>());

        var direct = await (
            from s in _db.Subjects.AsNoTracking()
            where bundleIds.Contains(s.BundleId)
            from u in _db.Units.AsNoTracking().Where(u => u.SubjectId == s.Id)
            from t in _db.Topics.AsNoTracking().Where(t => t.UnitId == u.Id)
            select new { s.BundleId, TopicId = t.Id }
        ).ToListAsync(ct);

        foreach (var row in direct)
            topicIdsByBundle[row.BundleId].Add(row.TopicId);

        var shared = await (
            from l in _db.SubjectSharedUnits.AsNoTracking()
            where bundleIds.Contains(l.Subject!.BundleId)
            from t in _db.Topics.AsNoTracking().Where(t => t.UnitId == l.UnitId)
            select new { BundleId = l.Subject!.BundleId, TopicId = t.Id }
        ).ToListAsync(ct);

        foreach (var row in shared)
            topicIdsByBundle[row.BundleId].Add(row.TopicId);

        return topicIdsByBundle.ToDictionary(kv => kv.Key, kv => kv.Value.Count);
    }
}
