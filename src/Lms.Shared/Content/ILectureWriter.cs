namespace Lms.Shared.Content;

/// <summary>Cross-module hook to attach a members-only recording lecture to a topic.</summary>
public interface ILectureWriter
{
    Task<Guid> UpsertMembersOnlyLectureAsync(
        Guid topicId,
        string title,
        string recordingUrl,
        Guid? liveClassId,
        CancellationToken ct = default);
}
