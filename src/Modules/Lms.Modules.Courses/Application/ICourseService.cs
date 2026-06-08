namespace Lms.Modules.Courses.Application;

public interface ICourseService
{
    Task<IReadOnlyList<BundleDto>> GetBundlesAsync(CancellationToken ct = default);
    Task<BundleDetailDto?> GetBundleAsync(Guid bundleId, CancellationToken ct = default);
    Task<IReadOnlyList<UnitDto>> GetUnitsAsync(Guid subjectId, CancellationToken ct = default);
    Task<IReadOnlyList<TopicDto>> GetTopicsAsync(Guid unitId, CancellationToken ct = default);
    Task<IReadOnlyList<TopicDto>> GetRecentTopicsAsync(int take, CancellationToken ct = default);
}
