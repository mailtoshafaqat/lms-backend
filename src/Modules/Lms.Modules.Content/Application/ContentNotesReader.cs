using Lms.Modules.Content.Infrastructure;
using Lms.Shared.Content;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Content.Application;

public sealed class ContentNotesReader : IContentNotesReader
{
    private readonly ContentDbContext _db;

    public ContentNotesReader(ContentDbContext db) => _db = db;

    public async Task<IReadOnlyList<NoteIngestItem>> GetNotesForTopicsAsync(
        IReadOnlyList<Guid> topicIds,
        CancellationToken ct = default)
    {
        if (topicIds.Count == 0) return Array.Empty<NoteIngestItem>();

        return await _db.Notes.AsNoTracking()
            .Where(n => topicIds.Contains(n.TopicId))
            .Select(n => new NoteIngestItem(n.TopicId, n.Id, n.Title, n.ContentHtml, n.StorageKey))
            .ToListAsync(ct);
    }
}
