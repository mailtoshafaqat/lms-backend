namespace Lms.Modules.Progress.Application;

public sealed record SaveLectureProgressRequest(
    int ProgressPercent,
    int PositionSec,
    Guid? TopicId);

public sealed record LectureProgressDto(
    Guid LectureId,
    Guid TopicId,
    int ProgressPercent,
    int PositionSec,
    DateTime LastWatchedAt);
