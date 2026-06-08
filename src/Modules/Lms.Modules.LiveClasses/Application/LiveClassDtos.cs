namespace Lms.Modules.LiveClasses.Application;

/// <summary>Computed lifecycle status based on the scheduled window and cancellation flag.</summary>
public enum LiveClassState
{
    Upcoming,
    Live,
    Ended,
    Cancelled
}

/// <summary>Student-facing view. Host-only fields (start url) are never included.
/// <c>State</c> is the string name of <see cref="LiveClassState"/>.</summary>
public sealed record LiveClassDto(
    Guid Id,
    Guid BundleId,
    string BundleTitle,
    string Title,
    string? Description,
    DateTime ScheduledStartUtc,
    int DurationMinutes,
    string State,
    string Provider,
    string JoinUrl,
    string? Passcode,
    string? RecordingUrl);

/// <summary>Admin-facing view, includes the host start url and meeting id.</summary>
public sealed record AdminLiveClassDto(
    Guid Id,
    Guid BundleId,
    string BundleTitle,
    string Title,
    string? Description,
    DateTime ScheduledStartUtc,
    int DurationMinutes,
    string State,
    string Provider,
    string JoinUrl,
    string? StartUrl,
    string? MeetingId,
    string? Passcode,
    string? RecordingUrl,
    Guid? RecordingTopicId,
    Guid? RecordingLectureId);

public sealed record AttachRecordingRequest(
    string RecordingUrl,
    Guid TopicId,
    string? LectureTitle);

/// <summary>When Zoom is configured the meeting is auto-created; otherwise <see cref="ManualJoinUrl"/>
/// is required.</summary>
public sealed record CreateLiveClassRequest(
    Guid BundleId,
    string Title,
    string? Description,
    DateTime ScheduledStartUtc,
    int DurationMinutes,
    string? ManualJoinUrl);
