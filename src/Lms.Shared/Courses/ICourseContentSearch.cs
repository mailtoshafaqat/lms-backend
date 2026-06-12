namespace Lms.Shared.Courses;

public sealed record ContentSearchHitDto(
    string Type,
    Guid Id,
    string Title,
    string Path,
    Guid? TopicId,
    Guid? SubjectId,
    Guid BundleId);

/// <summary>Full-text style search across published course hierarchy.</summary>
public interface ICourseContentSearch
{
    Task<IReadOnlyList<ContentSearchHitDto>> SearchAsync(
        string query,
        IReadOnlyList<Guid>? limitToBundleIds,
        int take = 20,
        CancellationToken ct = default);
}
