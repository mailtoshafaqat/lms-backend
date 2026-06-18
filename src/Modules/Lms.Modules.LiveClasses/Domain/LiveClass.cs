using Lms.Shared.Entities;

namespace Lms.Modules.LiveClasses.Domain;

public enum LiveClassProvider
{
    Manual = 0,
    Zoom = 1
}

/// <summary>A scheduled live session for a course (bundle). Backed by a Zoom meeting (created via
/// the tenant's Zoom account) or a manually supplied join link.</summary>
public sealed class LiveClass : TenantEntity
{
    public Guid BundleId { get; set; }
    public string BundleTitle { get; set; } = string.Empty;
    public Guid SubjectId { get; set; }
    public string SubjectTitle { get; set; } = string.Empty;
    public Guid HostUserId { get; set; }
    public string HostName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime ScheduledStartUtc { get; set; }
    public int DurationMinutes { get; set; }

    public LiveClassProvider Provider { get; set; } = LiveClassProvider.Manual;
    public string JoinUrl { get; set; } = string.Empty;
    public string? StartUrl { get; set; }
    public string? MeetingId { get; set; }
    public string? Passcode { get; set; }

    public Guid CreatedByUserId { get; set; }
    public bool IsCancelled { get; set; }

    public string? RecordingUrl { get; set; }
    public Guid? RecordingTopicId { get; set; }
    public Guid? RecordingLectureId { get; set; }

    public DateTime? ReminderNotifiedAtUtc { get; set; }
}
