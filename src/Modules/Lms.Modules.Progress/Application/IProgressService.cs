namespace Lms.Modules.Progress.Application;

public interface IProgressService
{
    Task<IReadOnlyList<GradeDto>> GetMyGradesAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<LeaderboardRowDto>> GetLeaderboardAsync(int take, Guid? currentUserId, CancellationToken ct = default);
}
