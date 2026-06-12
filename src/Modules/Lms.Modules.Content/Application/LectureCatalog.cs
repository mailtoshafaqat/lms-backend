using Lms.Modules.Content.Infrastructure;
using Lms.Shared.Content;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Content.Application;

public sealed class LectureCatalog : ILectureCatalog
{
    private readonly ContentDbContext _db;

    public LectureCatalog(ContentDbContext db) => _db = db;

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<Guid>>> GetLectureIdsByTopicAsync(
        IReadOnlyList<Guid> topicIds,
        CancellationToken ct = default)
    {
        if (topicIds.Count == 0)
            return new Dictionary<Guid, IReadOnlyList<Guid>>();

        var idSet = topicIds.Distinct().ToList();
        var rows = await _db.Lectures.AsNoTracking()
            .Where(l => idSet.Contains(l.TopicId))
            .Select(l => new { l.TopicId, l.Id })
            .ToListAsync(ct);

        return rows
            .GroupBy(r => r.TopicId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<Guid>)g.Select(x => x.Id).ToList());
    }
}
