namespace Lms.Modules.Progress.Application;

public sealed record BookmarkDto(
    Guid Id,
    string TargetType,
    Guid TargetId,
    string Title,
    string? Subtitle,
    Guid? TopicId,
    DateTime CreatedAt);

public sealed record CreateBookmarkRequest(
    string TargetType,
    Guid TargetId,
    string Title,
    string? Subtitle,
    Guid? TopicId);

public sealed record BookmarkStatusDto(bool IsBookmarked, Guid? BookmarkId);
