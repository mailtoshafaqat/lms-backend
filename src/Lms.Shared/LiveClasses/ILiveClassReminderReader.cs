namespace Lms.Shared.LiveClasses;

public sealed record LiveClassReminderCandidate(
    Guid Id,
    Guid TenantId,
    Guid BundleId,
    string Title,
    string SubjectTitle,
    DateTime ScheduledStartUtc,
    string JoinUrl);

/// <summary>Cross-module read/write for live-class reminder background jobs.</summary>
public interface ILiveClassReminderReader
{
    Task<IReadOnlyList<LiveClassReminderCandidate>> GetPendingRemindersAsync(
        DateTime windowStartUtc,
        DateTime windowEndUtc,
        CancellationToken ct = default);

    Task MarkReminderSentAsync(Guid liveClassId, CancellationToken ct = default);
}
