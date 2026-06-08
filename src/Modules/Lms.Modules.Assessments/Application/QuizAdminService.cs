using System.Text.Json;
using Lms.Modules.Assessments.Domain;
using Lms.Modules.Assessments.Infrastructure;
using Lms.Shared.Common;
using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Assessments.Application;

public sealed class QuizAdminService : IQuizAdminService
{
    private readonly AssessmentsDbContext _db;
    private readonly ITenantContext _tenant;

    public QuizAdminService(AssessmentsDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<AdminQuizDto?> GetAdminQuizAsync(Guid topicId, CancellationToken ct = default)
    {
        var quiz = await _db.Quizzes
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.TopicId == topicId, ct);

        if (quiz is null) return null;

        return new AdminQuizDto(
            quiz.Id,
            quiz.TopicId,
            quiz.Title,
            quiz.Questions
                .OrderBy(q => q.Order)
                .Select(ToDto)
                .ToList());
    }

    public async Task<Result<AdminQuestionDto>> AddQuestionAsync(Guid topicId, CreateQuestionRequest req, CancellationToken ct = default)
    {
        if (req.Options is null || req.Options.Count < 2)
            return Result<AdminQuestionDto>.Failure("At least two options are required.");
        if (!int.TryParse(req.CorrectKey, out var keyIdx) || keyIdx < 0 || keyIdx >= req.Options.Count)
            return Result<AdminQuestionDto>.Failure("CorrectKey must be a valid option index.");

        var quiz = await _db.Quizzes
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.TopicId == topicId, ct);

        if (quiz is null)
        {
            quiz = new Quiz
            {
                TenantId = _tenant.TenantId,
                TopicId = topicId,
                Title = "Daily Practice Test",
                Type = QuizType.DailyPracticeTest
            };
            _db.Quizzes.Add(quiz);
        }

        var order = quiz.Questions.Count == 0 ? 1 : quiz.Questions.Max(q => q.Order) + 1;

        var question = new Question
        {
            TenantId = _tenant.TenantId,
            Stem = req.Stem.Trim(),
            OptionsJson = JsonSerializer.Serialize(req.Options),
            CorrectKey = req.CorrectKey,
            Explanation = string.IsNullOrWhiteSpace(req.Explanation) ? null : req.Explanation.Trim(),
            Order = order
        };
        quiz.Questions.Add(question);

        await _db.SaveChangesAsync(ct);
        return Result<AdminQuestionDto>.Success(ToDto(question));
    }

    public async Task<bool> DeleteQuestionAsync(Guid questionId, CancellationToken ct = default)
    {
        var question = await _db.Questions.FindAsync([questionId], ct);
        if (question is null) return false;
        _db.Questions.Remove(question);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<Result<AdminQuestionDto>> UpdateQuestionAsync(
        Guid questionId, UpdateQuestionRequest req, CancellationToken ct = default)
    {
        if (req.Options is null || req.Options.Count < 2)
            return Result<AdminQuestionDto>.Failure("At least two options are required.");
        if (!int.TryParse(req.CorrectKey, out var keyIdx) || keyIdx < 0 || keyIdx >= req.Options.Count)
            return Result<AdminQuestionDto>.Failure("CorrectKey must be a valid option index.");

        var question = await _db.Questions.FindAsync([questionId], ct);
        if (question is null) return Result<AdminQuestionDto>.Failure("Question not found.");

        question.Stem = req.Stem.Trim();
        question.OptionsJson = JsonSerializer.Serialize(req.Options);
        question.CorrectKey = req.CorrectKey;
        question.Explanation = string.IsNullOrWhiteSpace(req.Explanation) ? null : req.Explanation.Trim();
        await _db.SaveChangesAsync(ct);
        return Result<AdminQuestionDto>.Success(ToDto(question));
    }

    public async Task<Result<bool>> UpdateQuizTitleAsync(
        Guid topicId, UpdateQuizTitleRequest req, CancellationToken ct = default)
    {
        var quiz = await _db.Quizzes.FirstOrDefaultAsync(q => q.TopicId == topicId, ct);
        if (quiz is null) return Result<bool>.Failure("Quiz not found.");
        quiz.Title = req.Title.Trim();
        await _db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> ReorderQuestionsAsync(
        Guid topicId, ReorderQuestionsRequest req, CancellationToken ct = default)
    {
        var quiz = await _db.Quizzes.Include(q => q.Questions).FirstOrDefaultAsync(q => q.TopicId == topicId, ct);
        if (quiz is null) return Result<bool>.Failure("Quiz not found.");

        var order = 1;
        foreach (var id in req.QuestionIds)
        {
            var q = quiz.Questions.FirstOrDefault(x => x.Id == id);
            if (q is null) return Result<bool>.Failure("Invalid question id in reorder list.");
            q.Order = order++;
        }

        await _db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    private static AdminQuestionDto ToDto(Question q) => new(
        q.Id,
        q.Stem,
        JsonSerializer.Deserialize<List<string>>(q.OptionsJson) ?? new List<string>(),
        q.CorrectKey,
        q.Explanation,
        q.Order);
}
