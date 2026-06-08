using Lms.Modules.Content.Infrastructure;
using Lms.Shared.Auth;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Content.Application;

public sealed class ContentService : IContentService
{
    private readonly ContentDbContext _db;
    private readonly ICurrentUser _user;

    public ContentService(ContentDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _user = currentUser;
    }

    public async Task<TopicContentDto> GetTopicContentAsync(Guid topicId, CancellationToken ct = default)
    {
        var isAuthed = _user.UserId is not null;
        var lectureRows = await _db.Lectures
            .Where(l => l.TopicId == topicId)
            .OrderBy(l => l.Order)
            .ToListAsync(ct);

        var lectures = lectureRows.Select(l =>
        {
            var locked = l.MembersOnly && !isAuthed;
            var url = locked
                ? null
                : l.Url ?? (l.StorageKey != null ? $"/api/v1/files/{l.StorageKey}" : null);
            return new LectureDto(l.Id, l.Title, url, l.DurationSec, l.Order, l.MembersOnly, locked);
        }).ToList();

        var notes = await _db.Notes
            .Where(n => n.TopicId == topicId)
            .OrderBy(n => n.Order)
            .Select(n => new NoteDto(
                n.Id,
                n.Title,
                n.ContentHtml,
                n.StorageKey != null ? $"/api/v1/files/{n.StorageKey}" : null,
                n.Order))
            .ToListAsync(ct);

        return new TopicContentDto(topicId, lectures, notes);
    }
}
