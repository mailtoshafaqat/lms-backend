using Lms.Shared.Common;

namespace Lms.Modules.Courses.Application;

public interface ICourseAdminService
{
    Task<BundleDto> CreateBundleAsync(CreateBundleRequest req, CancellationToken ct = default);
    Task<Result<BundleDto>> UpdateBundleAsync(Guid bundleId, UpdateBundleRequest req, CancellationToken ct = default);
    Task<Result<SubjectDto>> CreateSubjectAsync(Guid bundleId, CreateSubjectRequest req, CancellationToken ct = default);
    Task<Result<UnitDto>> CreateUnitAsync(Guid subjectId, CreateUnitRequest req, CancellationToken ct = default);
    Task<Result<TopicDto>> CreateTopicAsync(Guid unitId, CreateTopicRequest req, CancellationToken ct = default);

    Task<bool> DeleteBundleAsync(Guid id, CancellationToken ct = default);
    Task<bool> DeleteSubjectAsync(Guid id, CancellationToken ct = default);
    Task<bool> DeleteUnitAsync(Guid id, CancellationToken ct = default);
    Task<bool> DeleteTopicAsync(Guid id, CancellationToken ct = default);
}
