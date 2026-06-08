using Lms.Modules.Progress.Infrastructure;
using Lms.Shared.Users;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Progress.Application;

public sealed class ProgressService : IProgressService
{
    private readonly ProgressDbContext _db;
    private readonly IUserDirectory _users;

    public ProgressService(ProgressDbContext db, IUserDirectory users)
    {
        _db = db;
        _users = users;
    }

    public async Task<IReadOnlyList<GradeDto>> GetMyGradesAsync(Guid userId, CancellationToken ct = default)
    {
        var results = await _db.QuizResults
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.SubmittedAt)
            .Take(50)
            .ToListAsync(ct);

        return results
            .Select(r => new GradeDto(r.QuizId, r.TopicId, r.QuizTitle, r.Score, r.Total, r.Percentage, r.SubmittedAt))
            .ToList();
    }

    public async Task<IReadOnlyList<LeaderboardRowDto>> GetLeaderboardAsync(
        int take, Guid? currentUserId, CancellationToken ct = default)
    {
        // Points = sum of each user's best score per quiz (so re-attempting can't farm points).
        var perQuizBest = await _db.QuizResults
            .GroupBy(r => new { r.UserId, r.QuizId })
            .Select(g => new { g.Key.UserId, Best = g.Max(x => x.Score) })
            .ToListAsync(ct);

        var totals = perQuizBest
            .GroupBy(x => x.UserId)
            .Select(g => new { UserId = g.Key, Points = g.Sum(x => x.Best) })
            .OrderByDescending(x => x.Points)
            .Take(take)
            .ToList();

        var names = await _users.GetDisplayNamesAsync(totals.Select(t => t.UserId), ct);

        return totals
            .Select((t, i) => new LeaderboardRowDto(
                i + 1,
                t.UserId,
                names.TryGetValue(t.UserId, out var n) ? n : "Unknown",
                t.Points,
                currentUserId.HasValue && t.UserId == currentUserId.Value))
            .ToList();
    }
}
