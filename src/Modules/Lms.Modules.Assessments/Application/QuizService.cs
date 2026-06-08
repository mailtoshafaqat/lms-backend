using System.Text.Json;
using Lms.Modules.Assessments.Contracts;
using Lms.Modules.Assessments.Domain;
using Lms.Modules.Assessments.Infrastructure;
using Lms.Shared.Common;
using Lms.Shared.Events;
using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Assessments.Application;

public sealed class QuizService : IQuizService
{
    private readonly AssessmentsDbContext _db;
    private readonly IEventBus _events;
    private readonly ITenantContext _tenant;

    public QuizService(AssessmentsDbContext db, IEventBus events, ITenantContext tenant)
    {
        _db = db;
        _events = events;
        _tenant = tenant;
    }

    public async Task<QuizDto?> GetByTopicAsync(Guid topicId, CancellationToken ct = default)
    {
        var quiz = await _db.Quizzes
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.TopicId == topicId, ct);
        return quiz is null ? null : ToDto(quiz);
    }

    public async Task<QuizDto?> GetAsync(Guid quizId, CancellationToken ct = default)
    {
        var quiz = await _db.Quizzes
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.Id == quizId, ct);
        return quiz is null ? null : ToDto(quiz);
    }

    public async Task<Result<AttemptResultDto>> SubmitAsync(
        Guid quizId, Guid userId, SubmitAttemptRequest request, CancellationToken ct = default)
    {
        var quiz = await _db.Quizzes
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.Id == quizId, ct);

        if (quiz is null) return Result<AttemptResultDto>.Failure("Quiz not found.");

        var selectedByQuestion = request.Answers.ToDictionary(a => a.QuestionId, a => a.SelectedKey);
        var results = new List<QuestionResultDto>();
        var wrong = new List<Guid>();
        var score = 0;

        foreach (var q in quiz.Questions.OrderBy(x => x.Order))
        {
            selectedByQuestion.TryGetValue(q.Id, out var selected);
            var isCorrect = selected == q.CorrectKey;
            if (isCorrect) score++;
            else wrong.Add(q.Id);

            results.Add(new QuestionResultDto(
                q.Id, q.Stem, DeserializeOptions(q.OptionsJson),
                q.CorrectKey, selected, isCorrect, q.Explanation));
        }

        var attempt = new Attempt
        {
            TenantId = _tenant.TenantId,
            QuizId = quiz.Id,
            UserId = userId,
            Score = score,
            Total = quiz.Questions.Count,
            AnswersJson = JsonSerializer.Serialize(selectedByQuestion)
        };
        _db.Attempts.Add(attempt);
        await _db.SaveChangesAsync(ct);

        await _events.PublishAsync(new QuizSubmittedEvent(
            attempt.Id, attempt.TenantId, userId, quiz.Id, quiz.TopicId,
            quiz.Title, score, quiz.Questions.Count, wrong), ct);

        return Result<AttemptResultDto>.Success(
            new AttemptResultDto(attempt.Id, score, quiz.Questions.Count, results));
    }

    private static QuizDto ToDto(Quiz quiz) => new(
        quiz.Id,
        quiz.TopicId,
        quiz.Title,
        quiz.Questions
            .OrderBy(q => q.Order)
            .Select(q => new QuizQuestionDto(q.Id, q.Stem, DeserializeOptions(q.OptionsJson), q.Order))
            .ToList());

    private static IReadOnlyList<string> DeserializeOptions(string json) =>
        JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
}
