using Lms.Modules.QnA.Domain;
using Lms.Modules.QnA.Infrastructure;
using Lms.Shared.QnA;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.QnA.Application;

public sealed class DoubtSummaryReader : IDoubtSummaryReader
{
    private readonly QnADbContext _db;

    public DoubtSummaryReader(QnADbContext db) => _db = db;

    public async Task<StudentDoubtSummaryDto> GetSummaryForStudentSubjectAsync(
        Guid studentUserId, Guid subjectId, CancellationToken ct = default)
    {
        var threads = await _db.DoubtThreads.AsNoTracking()
            .Where(t => t.StudentUserId == studentUserId && t.SubjectId == subjectId)
            .Select(t => new { t.Status, t.UpdatedAt, t.CreatedAt })
            .ToListAsync(ct);

        if (threads.Count == 0)
            return new StudentDoubtSummaryDto(0, 0, null);

        var open = threads.Count(t => t.Status == DoubtThreadStatus.Open);
        var resolved = threads.Count - open;
        var last = threads
            .Select(t => t.UpdatedAt ?? t.CreatedAt)
            .Max();

        return new StudentDoubtSummaryDto(open, resolved, last);
    }
}
