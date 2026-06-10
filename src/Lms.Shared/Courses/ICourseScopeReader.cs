namespace Lms.Shared.Courses;

public sealed record TopicScope(
    Guid TopicId,
    string TopicTitle,
    Guid SubjectId,
    string SubjectTitle,
    Guid BundleId);

public sealed record SubjectScope(
    Guid SubjectId,
    string SubjectTitle,
    Guid BundleId);

/// <summary>Resolves topic/subject hierarchy for syllabus-scoped AI answers.</summary>
public interface ICourseScopeReader
{
    Task<TopicScope?> GetTopicScopeAsync(Guid topicId, CancellationToken ct = default);
    Task<SubjectScope?> GetSubjectScopeAsync(Guid subjectId, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetTopicIdsForSubjectAsync(Guid subjectId, CancellationToken ct = default);
}
