using System.Text.Json;
using Lms.Modules.Assessments.Contracts;
using Lms.Modules.Progress.Domain;
using Lms.Modules.Progress.Infrastructure;
using Lms.Shared.Assessments;
using Lms.Shared.Events;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Progress.Application;

public sealed class MistakeDiaryHandler : IEventHandler<QuizSubmittedEvent>
{
    private readonly ProgressDbContext _db;
    private readonly IQuestionReader _questions;

    public MistakeDiaryHandler(ProgressDbContext db, IQuestionReader questions)
    {
        _db = db;
        _questions = questions;
    }

    public async Task HandleAsync(QuizSubmittedEvent @event, CancellationToken cancellationToken = default)
    {
        if (@event.WrongQuestionIds.Count == 0) return;

        var snapshots = await _questions.GetByIdsAsync(@event.WrongQuestionIds, cancellationToken);
        var byId = snapshots.ToDictionary(q => q.Id);

        foreach (var questionId in @event.WrongQuestionIds)
        {
            if (!byId.TryGetValue(questionId, out var snap)) continue;

            var existing = await _db.MistakeEntries
                .FirstOrDefaultAsync(
                    m => m.UserId == @event.UserId && m.QuestionId == questionId && m.ResolvedAt == null,
                    cancellationToken);

            if (existing is null)
            {
                _db.MistakeEntries.Add(new MistakeEntry
                {
                    TenantId = @event.TenantId,
                    UserId = @event.UserId,
                    QuestionId = questionId,
                    TopicId = snap.TopicId,
                    QuizId = snap.QuizId,
                    QuizTitle = @event.QuizTitle,
                    Stem = snap.Stem,
                    OptionsJson = JsonSerializer.Serialize(snap.Options),
                    CorrectKey = snap.CorrectKey,
                    Explanation = snap.Explanation,
                    TimesWrong = 1
                });
            }
            else
            {
                existing.TimesWrong++;
                existing.LastSeenAt = DateTime.UtcNow;
                existing.QuizTitle = @event.QuizTitle;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
