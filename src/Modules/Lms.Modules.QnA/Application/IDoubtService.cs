using Lms.Shared.Common;
using Lms.Shared.Courses;

namespace Lms.Modules.QnA.Application;

public interface IDoubtService
{
    Task<IReadOnlyList<AssignedSubjectDto>> GetEnrolledSubjectsAsync(
        Guid userId, CancellationToken ct = default);

    Task<IReadOnlyList<DoubtThreadSummaryDto>> ListStudentThreadsAsync(
        Guid userId, CancellationToken ct = default);

    Task<Result<DoubtThreadDetailDto>> CreateThreadAsync(
        Guid userId, string role, CreateDoubtRequest request, CancellationToken ct = default);

    Task<Result<DoubtThreadDetailDto>> GetStudentThreadAsync(
        Guid userId, Guid threadId, CancellationToken ct = default);

    Task<Result<DoubtThreadDetailDto>> AddStudentMessageAsync(
        Guid userId, Guid threadId, string body, CancellationToken ct = default);

    Task<PagedResult<DoubtThreadSummaryDto>> ListAdminThreadsAsync(
        Guid userId, string role, string? statusFilter, PagedListQuery query, CancellationToken ct = default);

    Task<Result<DoubtThreadDetailDto>> GetAdminThreadAsync(
        Guid userId, string role, Guid threadId, CancellationToken ct = default);

    Task<Result<DoubtThreadDetailDto>> ReplyAsTeacherAsync(
        Guid userId, string role, Guid threadId, string body, CancellationToken ct = default);

    Task<Result<DoubtThreadDetailDto>> ResolveThreadAsync(
        Guid userId, string role, Guid threadId, CancellationToken ct = default);
}
