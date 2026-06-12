namespace Lms.Shared.Courses;

public sealed record TopicPathDto(
    Guid TopicId,
    string TopicTitle,
    Guid SubjectId,
    string SubjectTitle,
    Guid BundleId,
    string BundleTitle);

/// <summary>Resolves topic locations in the course tree for enrolled bundles.</summary>
public interface ICourseTopicCatalog
{
    Task<IReadOnlyList<TopicPathDto>> GetTopicPathsForBundlesAsync(
        IReadOnlyList<Guid> bundleIds,
        CancellationToken ct = default);
}
