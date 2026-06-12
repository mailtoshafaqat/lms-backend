namespace Lms.Shared.Content;

/// <summary>Read-only lecture index for progress and completion checks.</summary>
public interface ILectureCatalog
{
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<Guid>>> GetLectureIdsByTopicAsync(
        IReadOnlyList<Guid> topicIds,
        CancellationToken ct = default);
}
