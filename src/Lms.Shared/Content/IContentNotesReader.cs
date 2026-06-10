namespace Lms.Shared.Content;

public sealed record NoteIngestItem(
    Guid TopicId,
    Guid NoteId,
    string Title,
    string? ContentHtml,
    string? StorageKey);

/// <summary>Read-only access to study notes for Syllabus Mentor ingestion.</summary>
public interface IContentNotesReader
{
    Task<IReadOnlyList<NoteIngestItem>> GetNotesForTopicsAsync(
        IReadOnlyList<Guid> topicIds,
        CancellationToken ct = default);
}
