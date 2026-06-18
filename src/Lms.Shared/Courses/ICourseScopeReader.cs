namespace Lms.Shared.Courses;

public sealed record OrderedTopicRef(Guid TopicId, string TopicTitle);

public sealed record TopicScope(
    Guid TopicId,
    string TopicTitle,
    Guid SubjectId,
    string SubjectTitle,
    Guid BundleId);

public sealed record SubjectScope(
    Guid SubjectId,
    string SubjectTitle,
    Guid BundleId,
    string BundleTitle);

/// <summary>Resolves topic/subject hierarchy for syllabus-scoped AI answers.</summary>
public interface ICourseScopeReader
{
    Task<TopicScope?> GetTopicScopeAsync(Guid topicId, CancellationToken ct = default);
    Task<SubjectScope?> GetSubjectScopeAsync(Guid subjectId, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetTopicIdsForSubjectAsync(Guid subjectId, CancellationToken ct = default);

    /// <summary>Topics in syllabus order (own units then shared units, each ordered by Order).</summary>
    Task<IReadOnlyList<OrderedTopicRef>> GetOrderedTopicsForSubjectAsync(
        Guid subjectId, CancellationToken ct = default);

    Task<IReadOnlyList<Guid>> GetTopicIdsForUnitAsync(Guid unitId, CancellationToken ct = default);

    Task<IReadOnlyDictionary<Guid, TopicScope>> GetTopicScopesAsync(
        IReadOnlyList<Guid> topicIds, CancellationToken ct = default);

    Task<IReadOnlyDictionary<Guid, int>> GetTopicCountsByBundleAsync(
        IReadOnlyList<Guid> bundleIds, CancellationToken ct = default);

    /// <summary>Resolves the bundle that owns syllabus content under a unit (direct or shared).</summary>
    Task<Guid?> GetBundleIdForUnitAsync(Guid unitId, CancellationToken ct = default);
}
