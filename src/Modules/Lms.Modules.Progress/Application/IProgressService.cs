using Lms.Shared.Common;

namespace Lms.Modules.Progress.Application;

public interface IProgressService
{
    Task<IReadOnlyList<GradeDto>> GetMyGradesAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<LeaderboardRowDto>> GetLeaderboardAsync(int take, Guid? currentUserId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<LeaderboardRowDto>>> GetSubjectLeaderboardAsync(
        Guid userId, string role, Guid subjectId, int take, CancellationToken ct = default);

    Task<Result<SubjectProgressDto>> GetSubjectProgressAsync(
        Guid userId, string role, Guid subjectId, CancellationToken ct = default);

    Task<Result<StudentDetailDto>> GetStudentDetailAsync(
        Guid userId, string role, Guid subjectId, Guid studentUserId, CancellationToken ct = default);

    Task<DashboardOverviewDto> GetDashboardOverviewAsync(Guid userId, CancellationToken ct = default);
}
