using System.Text.Json;
using Lms.Modules.Assessments.Infrastructure;
using Lms.Shared.Assessments;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Assessments.Application;

public sealed class QuestionReader : IQuestionReader
{
    private readonly AssessmentsDbContext _db;

    public QuestionReader(AssessmentsDbContext db) => _db = db;

    public async Task<IReadOnlyList<QuestionSnapshot>> GetByIdsAsync(
        IEnumerable<Guid> questionIds, CancellationToken ct = default)
    {
        var ids = questionIds.Distinct().ToList();
        if (ids.Count == 0) return [];

        var rows = await _db.Questions.AsNoTracking()
            .Where(q => ids.Contains(q.Id))
            .Join(_db.Quizzes.AsNoTracking(), q => q.QuizId, quiz => quiz.Id, (q, quiz) => new { q, quiz })
            .ToListAsync(ct);

        return rows.Select(x => new QuestionSnapshot(
            x.q.Id,
            x.quiz.TopicId ?? Guid.Empty,
            x.quiz.Id,
            x.q.Stem,
            JsonSerializer.Deserialize<List<string>>(x.q.OptionsJson) ?? [],
            x.q.CorrectKey,
            x.q.Explanation)).ToList();
    }

    public async Task<IReadOnlyList<QuestionSnapshot>> GetByTopicIdsAsync(
        IEnumerable<Guid> topicIds, int take, CancellationToken ct = default)
    {
        var ids = topicIds.Distinct().ToList();
        if (ids.Count == 0 || take <= 0) return [];

        var rows = await _db.Questions.AsNoTracking()
            .Join(_db.Quizzes.AsNoTracking(), q => q.QuizId, quiz => quiz.Id, (q, quiz) => new { q, quiz })
            .Where(x => x.quiz.TopicId != null && ids.Contains(x.quiz.TopicId.Value))
            .Take(Math.Min(take * 3, 100))
            .ToListAsync(ct);

        rows = rows.OrderBy(_ => Random.Shared.Next()).Take(take).ToList();

        return rows.Select(x => new QuestionSnapshot(
            x.q.Id,
            x.quiz.TopicId!.Value,
            x.quiz.Id,
            x.q.Stem,
            JsonSerializer.Deserialize<List<string>>(x.q.OptionsJson) ?? [],
            x.q.CorrectKey,
            x.q.Explanation)).ToList();
    }
}
