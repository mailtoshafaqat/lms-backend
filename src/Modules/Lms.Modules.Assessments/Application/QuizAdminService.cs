using System.Text.Json;
using Lms.Modules.Assessments.Domain;
using Lms.Modules.Assessments.Infrastructure;
using Lms.Shared.Common;
using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Lms.Modules.Assessments.Application;

public sealed class QuizAdminService : IQuizAdminService
{
    private readonly AssessmentsDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ITenantFeaturesProvider _features;

    public QuizAdminService(
        AssessmentsDbContext db,
        ITenantContext tenant,
        ITenantFeaturesProvider features)
    {
        _db = db;
        _tenant = tenant;
        _features = features;
    }

    public async Task<AdminQuizDto?> GetAdminQuizAsync(Guid topicId, CancellationToken ct = default)
    {
        var quiz = await _db.Quizzes
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.TopicId == topicId, ct);

        if (quiz is null) return null;

        return MapAdminQuiz(quiz);
    }

    public async Task<Result<AdminQuestionDto>> AddQuestionAsync(Guid topicId, CreateQuestionRequest req, CancellationToken ct = default)
    {
        if (req.Options is null || req.Options.Count < 2)
            return Result<AdminQuestionDto>.Failure("At least two options are required.");
        if (!int.TryParse(req.CorrectKey, out var keyIdx) || keyIdx < 0 || keyIdx >= req.Options.Count)
            return Result<AdminQuestionDto>.Failure("CorrectKey must be a valid option index.");

        var quizId = await _db.Quizzes
            .Where(q => q.TopicId == topicId)
            .Select(q => q.Id)
            .FirstOrDefaultAsync(ct);

        if (quizId == Guid.Empty)
        {
            var quiz = new Quiz
            {
                TenantId = _tenant.TenantId,
                TopicId = topicId,
                Title = "Daily Practice Test",
                Type = QuizType.DailyPracticeTest
            };
            _db.Quizzes.Add(quiz);
            await _db.SaveChangesAsync(ct);
            quizId = quiz.Id;
        }

        var maxOrder = await _db.Questions
            .Where(q => q.QuizId == quizId)
            .Select(q => (int?)q.Order)
            .MaxAsync(ct) ?? 0;

        var question = new Question
        {
            TenantId = _tenant.TenantId,
            QuizId = quizId,
            Stem = req.Stem.Trim(),
            OptionsJson = JsonSerializer.Serialize(req.Options),
            CorrectKey = req.CorrectKey,
            Explanation = string.IsNullOrWhiteSpace(req.Explanation) ? null : req.Explanation.Trim(),
            Order = maxOrder + 1,
            IsPyq = req.IsPyq,
            PyqYear = req.IsPyq ? req.PyqYear : null,
            PyqExam = req.IsPyq && !string.IsNullOrWhiteSpace(req.PyqExam) ? req.PyqExam.Trim() : null
        };
        _db.Questions.Add(question);
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
        question.IsPyq = req.IsPyq;
        question.PyqYear = req.IsPyq ? req.PyqYear : null;
        question.PyqExam = req.IsPyq && !string.IsNullOrWhiteSpace(req.PyqExam) ? req.PyqExam.Trim() : null;
        await _db.SaveChangesAsync(ct);
        return Result<AdminQuestionDto>.Success(ToDto(question));
    }

    public async Task<QuizAnalyticsDto?> GetQuizAnalyticsAsync(Guid topicId, CancellationToken ct = default)
    {
        var quiz = await _db.Quizzes
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.TopicId == topicId, ct);

        if (quiz is null) return null;

        var attempts = await _db.Attempts.AsNoTracking()
            .Where(a => a.QuizId == quiz.Id && a.SubmittedAt != null)
            .Select(a => a.AnswersJson)
            .ToListAsync(ct);

        var questionStats = quiz.Questions
            .OrderBy(q => q.Order)
            .Select(q =>
            {
                var attemptCount = 0;
                var wrongCount = 0;

                foreach (var answersJson in attempts)
                {
                    if (!TryGetSelectedKey(answersJson, q.Id, out var selected)) continue;
                    attemptCount++;
                    if (selected != q.CorrectKey) wrongCount++;
                }

                var wrongPct = attemptCount == 0 ? 0 : (int)Math.Round(wrongCount * 100.0 / attemptCount);
                return new QuestionAnalyticsDto(q.Id, q.Stem, attemptCount, wrongCount, wrongPct);
            })
            .OrderByDescending(x => x.WrongPercentage)
            .ThenByDescending(x => x.AttemptCount)
            .ToList();

        return new QuizAnalyticsDto(quiz.Id, quiz.TopicId, quiz.Title, attempts.Count, questionStats);
    }

    private static bool TryGetSelectedKey(string answersJson, Guid questionId, out string? selected)
    {
        selected = null;
        try
        {
            var map = JsonSerializer.Deserialize<Dictionary<Guid, string>>(answersJson);
            if (map is null) return false;
            return map.TryGetValue(questionId, out selected);
        }
        catch
        {
            return false;
        }
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

    public async Task<Result<AdminQuizDto>> UpdateQuizSettingsAsync(
        Guid topicId, UpdateQuizSettingsRequest req, CancellationToken ct = default)
    {
        if (req.TimeLimitMinutes is < 0)
            return Result<AdminQuizDto>.Failure("Time limit cannot be negative.");

        if (req.AvailableFromUtc is not null && req.AvailableUntilUtc is not null
            && req.AvailableUntilUtc <= req.AvailableFromUtc)
            return Result<AdminQuizDto>.Failure("Available until must be after available from.");

        var quiz = await _db.Quizzes
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.TopicId == topicId, ct);

        if (quiz is null)
            return Result<AdminQuizDto>.Failure("Add at least one question before configuring schedule.");

        if (req.ResultVisibility is not null)
        {
            var mode = AssessmentResultPolicy.ParseMode(req.ResultVisibility);
            if (mode is null)
                return Result<AdminQuizDto>.Failure("Invalid result visibility mode.");

            if (mode == ResultVisibilityMode.AfterClose && req.AvailableUntilUtc is null && quiz.AvailableUntilUtc is null)
                return Result<AdminQuizDto>.Failure("After-close visibility requires an end date.");

            if (mode != ResultVisibilityMode.ManualPublish)
                quiz.ResultsPublishedAtUtc = null;

            quiz.ResultVisibility = mode.Value;
        }

        quiz.TimeLimitMinutes = req.TimeLimitMinutes is > 0 ? req.TimeLimitMinutes : null;
        quiz.AvailableFromUtc = req.AvailableFromUtc;
        quiz.AvailableUntilUtc = req.AvailableUntilUtc;

        if (quiz.ResultVisibility == ResultVisibilityMode.AfterClose && quiz.AvailableUntilUtc is null)
            return Result<AdminQuizDto>.Failure("After-close visibility requires an end date.");

        if (req.ShowExplanations is not null)
            quiz.ShowExplanations = req.ShowExplanations.Value;
        if (req.NotifyTeachersOnBatchComplete is not null)
            quiz.NotifyTeachersOnBatchComplete = req.NotifyTeachersOnBatchComplete.Value;
        if (req.BatchCompleteThresholdPercent is not null)
            quiz.BatchCompleteThresholdPercent = Math.Clamp(req.BatchCompleteThresholdPercent.Value, 1, 100);

        await _db.SaveChangesAsync(ct);

        return Result<AdminQuizDto>.Success(MapAdminQuiz(quiz));
    }

    public async Task<Result<AdminQuizDto>> PublishResultsAsync(Guid topicId, CancellationToken ct = default)
    {
        var quiz = await _db.Quizzes
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.TopicId == topicId, ct);

        if (quiz is null)
            return Result<AdminQuizDto>.Failure("Quiz not found.");

        if (quiz.ResultVisibility != ResultVisibilityMode.ManualPublish)
            return Result<AdminQuizDto>.Failure("Results can only be published when visibility is ManualPublish.");

        quiz.ResultsPublishedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Result<AdminQuizDto>.Success(MapAdminQuiz(quiz));
    }

    public Task<Result<McqImportPreviewDto>> PreviewMcqImportAsync(
        IReadOnlyList<McqImportRowInput> rows, CancellationToken ct = default) =>
        Task.FromResult(Result<McqImportPreviewDto>.Success(McqImportValidator.Preview(rows)));

    public async Task<Result<McqImportResultDto>> ImportMcqAsync(
        Guid topicId, McqImportRequest req, CancellationToken ct = default)
    {
        var flags = await _features.GetAsync(_tenant.TenantId, ct);
        if (flags is not null && !flags.McqBulkImportEnabled)
            return Result<McqImportResultDto>.Failure("MCQ bulk import is disabled for this institute.");

        if (req.Rows is null || req.Rows.Count == 0)
            return Result<McqImportResultDto>.Failure("No rows to import.");

        var preview = McqImportValidator.Preview(req.Rows);
        if (preview.InvalidCount > 0)
            return Result<McqImportResultDto>.Failure(
                $"{preview.InvalidCount} row(s) have errors. Fix them before importing.");

        var imported = new List<AdminQuestionDto>();
        foreach (var row in preview.Rows.Where(r => r.IsValid))
        {
            var create = McqImportValidator.ToCreateRequest(row);
            var added = await AddQuestionAsync(topicId, create, ct);
            if (!added.Succeeded)
                return Result<McqImportResultDto>.Failure(added.Error ?? "Import failed.");

            imported.Add(added.Value!);
        }

        return Result<McqImportResultDto>.Success(
            new McqImportResultDto(imported.Count, 0, imported));
    }

    private static AdminQuizDto MapAdminQuiz(Quiz quiz) => new(
        quiz.Id,
        quiz.TopicId,
        quiz.Title,
        quiz.TimeLimitMinutes,
        quiz.AvailableFromUtc,
        quiz.AvailableUntilUtc,
        quiz.ResultVisibility.ToString(),
        quiz.ShowExplanations,
        quiz.ResultsPublishedAtUtc,
        quiz.NotifyTeachersOnBatchComplete,
        quiz.BatchCompleteThresholdPercent,
        quiz.Questions
            .OrderBy(q => q.Order)
            .Select(ToDto)
            .ToList());

    private static AdminQuestionDto ToDto(Question q) => new(
        q.Id,
        q.Stem,
        JsonSerializer.Deserialize<List<string>>(q.OptionsJson) ?? new List<string>(),
        q.CorrectKey,
        q.Explanation,
        q.Order,
        q.IsPyq,
        q.PyqYear,
        q.PyqExam);
}
