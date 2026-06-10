using Lms.Shared.Common;

namespace Lms.Shared.Courses;

public sealed record AssignedSubjectDto(
    Guid SubjectId,
    string SubjectTitle,
    Guid BundleId,
    string BundleTitle);

public sealed record TeacherSubjectAssignmentDto(
    Guid UserId,
    IReadOnlyList<Guid> SubjectIds);

public interface ISubjectAccessService
{
    bool HasInstituteWideAccess(string? role);

    Task<bool> CanManageSubjectAsync(Guid userId, string role, Guid subjectId, CancellationToken ct = default);

    Task<bool> CanManageUnitAsync(Guid userId, string role, Guid unitId, CancellationToken ct = default);

    Task<bool> CanManageTopicAsync(Guid userId, string role, Guid topicId, CancellationToken ct = default);

    Task<bool> IsTeacherAssignedAsync(Guid teacherUserId, Guid subjectId, CancellationToken ct = default);

    Task<IReadOnlyList<Guid>> GetTeacherIdsForSubjectAsync(Guid subjectId, CancellationToken ct = default);

    Task<IReadOnlyList<AssignedSubjectDto>> GetAssignedSubjectsAsync(
        Guid userId, string role, CancellationToken ct = default);

    Task<IReadOnlyList<TeacherSubjectAssignmentDto>> ListAssignmentsAsync(CancellationToken ct = default);

    Task<Result> SetTeacherSubjectsAsync(Guid teacherUserId, IReadOnlyList<Guid> subjectIds, CancellationToken ct = default);
}
