using System.Text.Json;
using Lms.Modules.Progress.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Progress.Application;

public sealed class MistakeDiaryService : IMistakeDiaryService
{
    private readonly ProgressDbContext _db;

    public MistakeDiaryService(ProgressDbContext db) => _db = db;

    public async Task<IReadOnlyList<MistakeDto>> ListAsync(
        Guid userId, bool includeResolved = false, CancellationToken ct = default)
    {
        var query = _db.MistakeEntries.AsNoTracking().Where(m => m.UserId == userId);
        if (!includeResolved) query = query.Where(m => m.ResolvedAt == null);

        var rows = await query.OrderByDescending(m => m.LastSeenAt).ToListAsync(ct);
        return rows.Select(Map).ToList();
    }

    public async Task<bool> ResolveAsync(Guid userId, Guid mistakeId, CancellationToken ct = default)
    {
        var row = await _db.MistakeEntries.FirstOrDefaultAsync(m => m.Id == mistakeId && m.UserId == userId, ct);
        if (row is null) return false;
        row.ResolvedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static MistakeDto Map(Domain.MistakeEntry m) => new(
        m.Id,
        m.QuestionId,
        m.TopicId,
        m.QuizId,
        m.QuizTitle,
        m.Stem,
        JsonSerializer.Deserialize<List<string>>(m.OptionsJson) ?? [],
        m.CorrectKey,
        m.LastSelectedKey,
        m.Explanation,
        m.TimesWrong,
        m.LastSeenAt);
}
