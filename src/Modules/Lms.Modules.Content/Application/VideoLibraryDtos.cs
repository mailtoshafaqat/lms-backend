namespace Lms.Modules.Content.Application;

public sealed record VideoLibraryItemDto(
    Guid LectureId,
    string LectureTitle,
    string? PlayUrl,
    int DurationSec,
    Guid TopicId,
    string TopicTitle,
    string SubjectTitle,
    string BundleTitle,
    Guid BundleId);

public sealed record VideoLibraryDto(
    bool VideosOnlyStudent,
    IReadOnlyList<VideoLibraryItemDto> Items);
