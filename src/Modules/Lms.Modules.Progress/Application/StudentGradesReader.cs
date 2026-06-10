using Lms.Modules.Progress.Infrastructure;
using Lms.Shared.Progress;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Progress.Application;

public sealed class StudentGradesReader : IStudentGradesReader
{
    private readonly ProgressDbContext _db;

    public StudentGradesReader(ProgressDbContext db) => _db = db;

    public async Task<IReadOnlyList<StudentGradeDto>> GetRecentGradesAsync(
        Guid studentUserId, int take = 50, CancellationToken ct = default)
    {
        var results = await _db.QuizResults.AsNoTracking()
            .Where(r => r.UserId == studentUserId)
            .OrderByDescending(r => r.SubmittedAt)
            .Take(take)
            .ToListAsync(ct);

        return results
            .Select(r => new StudentGradeDto(
                r.QuizId, r.TopicId, r.QuizTitle, r.Score, r.Total, r.Percentage, r.SubmittedAt))
            .ToList();
    }
}
