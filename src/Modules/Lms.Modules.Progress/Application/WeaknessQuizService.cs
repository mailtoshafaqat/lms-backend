using System.Text.Json;
using Lms.Modules.Progress.Infrastructure;
using Lms.Shared.Assessments;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Progress.Application;

public sealed class WeaknessQuizService : IWeaknessQuizService
{
    private readonly ProgressDbContext _db;
    private readonly IQuestionReader _questions;

    public WeaknessQuizService(ProgressDbContext db, IQuestionReader questions)
    {
        _db = db;
        _questions = questions;
    }

    public async Task<WeaknessQuizDto?> BuildAsync(Guid userId, int count = 10, CancellationToken ct = default)
    {
        var take = Math.Clamp(count, 5, 25);
        var mistakes = await _db.MistakeEntries.AsNoTracking()
            .Where(m => m.UserId == userId && m.ResolvedAt == null)
            .OrderByDescending(m => m.TimesWrong)
            .ThenByDescending(m => m.LastSeenAt)
            .ToListAsync(ct);

        var selected = mistakes
            .GroupBy(m => m.QuestionId)
            .Select(g => g.First())
            .Take(take)
            .ToList();

        var source = "mistakes";

        if (selected.Count < 5)
        {
            var weakTopicIds = await GetWeakTopicIdsAsync(userId, ct);
            if (weakTopicIds.Count > 0)
            {
                var existingIds = selected.Select(m => m.QuestionId).ToHashSet();
                var extra = await LoadQuestionsForTopicsAsync(weakTopicIds, existingIds, take - selected.Count, ct);
                foreach (var q in extra)
                {
                    selected.Add(new Domain.MistakeEntry
                    {
                        QuestionId = q.Id,
                        TopicId = q.TopicId,
                        Stem = q.Stem,
                        OptionsJson = JsonSerializer.Serialize(q.Options),
                        CorrectKey = q.CorrectKey,
                        Explanation = q.Explanation
                    });
                }

                if (extra.Count > 0) source = selected.Count > extra.Count ? "mixed" : "weak-topics";
            }
        }

        if (selected.Count == 0) return null;

        var quizQuestions = selected
            .Select((m, index) => new WeaknessQuizQuestionDto(
                m.QuestionId,
                m.Stem,
                JsonSerializer.Deserialize<List<string>>(m.OptionsJson) ?? [],
                index + 1))
            .ToList();

        return new WeaknessQuizDto(Guid.NewGuid(), "Weakness practice", source, quizQuestions);
    }

    public async Task<WeaknessQuizResultDto> SubmitAsync(
        Guid userId, SubmitWeaknessQuizRequest request, CancellationToken ct = default)
    {
        if (request.Answers.Count == 0)
            throw new InvalidOperationException("No answers submitted.");

        var questionIds = request.Answers.Select(a => a.QuestionId).Distinct().ToList();
        var snapshots = (await _questions.GetByIdsAsync(questionIds, ct))
            .ToDictionary(q => q.Id);

        var mistakes = await _db.MistakeEntries
            .Where(m => m.UserId == userId && questionIds.Contains(m.QuestionId) && m.ResolvedAt == null)
            .ToListAsync(ct);
        var mistakesByQuestion = mistakes
            .GroupBy(m => m.QuestionId)
            .ToDictionary(g => g.Key, g => g.First());

        var results = new List<WeaknessQuestionResultDto>();
        var score = 0;
        var resolved = 0;

        foreach (var answer in request.Answers)
        {
            if (!snapshots.TryGetValue(answer.QuestionId, out var snap))
                continue;

            var isCorrect = string.Equals(
                snap.CorrectKey,
                answer.SelectedKey?.Trim(),
                StringComparison.OrdinalIgnoreCase);

            if (isCorrect) score++;

            if (isCorrect && mistakesByQuestion.TryGetValue(answer.QuestionId, out var mistake))
            {
                mistake.ResolvedAt = DateTime.UtcNow;
                resolved++;
            }

            results.Add(new WeaknessQuestionResultDto(
                snap.Id,
                snap.Stem,
                snap.Options,
                snap.CorrectKey,
                answer.SelectedKey,
                isCorrect,
                snap.Explanation));
        }

        await _db.SaveChangesAsync(ct);

        return new WeaknessQuizResultDto(score, results.Count, resolved, results);
    }

    private async Task<IReadOnlyList<Guid>> GetWeakTopicIdsAsync(Guid userId, CancellationToken ct)
    {
        var rows = await _db.QuizResults.AsNoTracking()
            .Where(r => r.UserId == userId)
            .GroupBy(r => r.TopicId)
            .Select(g => new
            {
                TopicId = g.Key,
                Avg = g.Average(x => x.Total == 0 ? 0.0 : 100.0 * x.Score / x.Total)
            })
            .Where(x => x.Avg < 60)
            .OrderBy(x => x.Avg)
            .Take(5)
            .Select(x => x.TopicId)
            .ToListAsync(ct);
        return rows;
    }

    private async Task<IReadOnlyList<QuestionSnapshot>> LoadQuestionsForTopicsAsync(
        IReadOnlyList<Guid> topicIds,
        HashSet<Guid> excludeQuestionIds,
        int needed,
        CancellationToken ct)
    {
        if (needed <= 0) return [];

        var snapshots = await _questions.GetByTopicIdsAsync(topicIds, needed * 3, ct);
        return snapshots.Where(q => !excludeQuestionIds.Contains(q.Id)).Take(needed).ToList();
    }
}
