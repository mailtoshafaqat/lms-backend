namespace Lms.Modules.Content.Application;

public sealed record LectureDto(
    Guid Id,
    string Title,
    string? Url,
    int DurationSec,
    int Order,
    bool MembersOnly,
    bool Locked);

public sealed record NoteDto(Guid Id, string Title, string? ContentHtml, string? DownloadUrl, int Order);

public sealed record TopicContentDto(
    Guid TopicId,
    IReadOnlyList<LectureDto> Lectures,
    IReadOnlyList<NoteDto> Notes);
