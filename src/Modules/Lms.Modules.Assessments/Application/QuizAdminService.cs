using System.Text.Json;
using Lms.Modules.Assessments.Domain;
using Lms.Modules.Assessments.Infrastructure;
using Lms.Shared.Common;
using Lms.Shared.Courses;
using Lms.Shared.Enrollments;
using Lms.Shared.Notifications;
using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Lms.Modules.Assessments.Application;

public sealed class QuizAdminService : IQuizAdminService
{
    private readonly AssessmentsDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ITenantFeaturesProvider _features;
    private readonly ICourseScopeReader _scope;
    private readonly IEnrollmentReader _enrollments;
    private readonly IStudentNotificationService _notifications;

    public QuizAdminService(
        AssessmentsDbContext db,
        ITenantContext tenant,
        ITenantFeaturesProvider features,
        ICourseScopeReader scope,
        IEnrollmentReader enrollments,
        IStudentNotificationService notifications)
    {
        _db = db;
        _tenant = tenant;
        _features = features;
        _scope = scope;
        _enrollments = enrollments;
        _notifications = notifications;
    }

    public async Task<PagedResult<QuestionSearchHitDto>> SearchQuestionsAsync(
        string query, int page, int pageSize, CancellationToken ct = default)
    {
        var term = query.Trim();
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        if (term.Length < 2)
            return new PagedResult<QuestionSearchHitDto>([], normalizedPage, normalizedSize, 0);

        var baseQuery = _db.Questions.AsNoTracking()
            .Where(q => q.Stem.Contains(term));

        var total = await baseQuery.CountAsync(ct);
        var rows = await baseQuery
            .Include(q => q.Quiz)
            .OrderBy(q => q.Stem)
            .Skip((normalizedPage - 1) * normalizedSize)
            .Take(normalizedSize)
            .ToListAsync(ct);

        var topicIds = rows
            .Where(r => r.Quiz?.TopicId is Guid)
            .Select(r => r.Quiz!.TopicId!.Value)
            .Distinct()
            .ToList();
        var scopes = await _scope.GetTopicScopesAsync(topicIds, ct);

        var data = rows.Select(r =>
        {
            TopicScope? scope = r.Quiz?.TopicId is Guid tid && scopes.TryGetValue(tid, out var s) ? s : null;
            return new QuestionSearchHitDto(
                r.Id,
                r.Stem,
                r.Quiz?.TopicId,
                scope?.TopicTitle,
                scope?.SubjectTitle,
                null,
                r.Order);
        }).ToList();

        return new PagedResult<QuestionSearchHitDto>(data, normalizedPage, normalizedSize, total);
    }

    public async Task<AdminQuizDto?> GetAdminQuizAsync(Guid topicId, CancellationToken ct = default)
    {
        var quiz = await _db.Quizzes
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.TopicId == topicId && q.Type == QuizType.DailyPracticeTest, ct);

        if (quiz is null) return null;

        return await MapAdminQuizAsync(quiz, ct);
    }

    public async Task<Result<AdminQuestionDto>> AddQuestionAsync(Guid topicId, CreateQuestionRequest req, CancellationToken ct = default)
    {
        if (req.Options is null || req.Options.Count < 2)
            return Result<AdminQuestionDto>.Failure("At least two options are required.");
        if (!int.TryParse(req.CorrectKey, out var keyIdx) || keyIdx < 0 || keyIdx >= req.Options.Count)
            return Result<AdminQuestionDto>.Failure("CorrectKey must be a valid option index.");

        var quizId = await _db.Quizzes
            .Where(q => q.TopicId == topicId && q.Type == QuizType.DailyPracticeTest)
            .Select(q => q.Id)
            .FirstOrDefaultAsync(ct);

        var isNewQuiz = quizId == Guid.Empty;
        if (isNewQuiz)
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

        var priorCount = await _db.Questions.CountAsync(q => q.QuizId == quizId, ct);

        var maxOrder = await _db.Questions
            .Where(q => q.QuizId == quizId)
            .Select(q => (int?)q.Order)
            .MaxAsync(ct) ?? 0;

        var difficulty = QuizQuestionAssembler.ParseDifficulty(req.Difficulty) ?? QuestionDifficulty.Medium;
        var question = new Question
        {
            TenantId = _tenant.TenantId,
            QuizId = quizId,
            Stem = req.Stem.Trim(),
            OptionsJson = JsonSerializer.Serialize(req.Options),
            CorrectKey = req.CorrectKey,
            Explanation = string.IsNullOrWhiteSpace(req.Explanation) ? null : req.Explanation.Trim(),
            Order = maxOrder + 1,
            Difficulty = difficulty,
            IsPyq = req.IsPyq,
            PyqYear = req.IsPyq ? req.PyqYear : null,
            PyqExam = req.IsPyq && !string.IsNullOrWhiteSpace(req.PyqExam) ? req.PyqExam.Trim() : null
        };
        _db.Questions.Add(question);
        await _db.SaveChangesAsync(ct);

        if (priorCount == 0)
            await TryNotifyQuizAvailableAsync(topicId, ct);

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
        if (req.Difficulty is not null)
        {
            var parsed = QuizQuestionAssembler.ParseDifficulty(req.Difficulty);
            if (parsed is null)
                return Result<AdminQuestionDto>.Failure("Invalid difficulty. Use Easy, Medium, or Hard.");
            question.Difficulty = parsed.Value;
        }
        await _db.SaveChangesAsync(ct);
        return Result<AdminQuestionDto>.Success(ToDto(question));
    }

    public async Task<QuizAnalyticsDto?> GetQuizAnalyticsAsync(Guid topicId, CancellationToken ct = default)
    {
        var quiz = await _db.Quizzes
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.TopicId == topicId && q.Type == QuizType.DailyPracticeTest, ct);

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

        return new QuizAnalyticsDto(quiz.Id, quiz.TopicId!.Value, quiz.Title, attempts.Count, questionStats);
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
        var quiz = await _db.Quizzes.FirstOrDefaultAsync(
            q => q.TopicId == topicId && q.Type == QuizType.DailyPracticeTest, ct);
        if (quiz is null) return Result<bool>.Failure("Quiz not found.");
        quiz.Title = req.Title.Trim();
        await _db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> ReorderQuestionsAsync(
        Guid topicId, ReorderQuestionsRequest req, CancellationToken ct = default)
    {
        var quiz = await _db.Quizzes.Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.TopicId == topicId && q.Type == QuizType.DailyPracticeTest, ct);
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
            .FirstOrDefaultAsync(q => q.TopicId == topicId && q.Type == QuizType.DailyPracticeTest, ct);

        if (quiz is null)
            return Result<AdminQuizDto>.Failure("Add at least one question before configuring schedule.");

        if (req.DifficultyFilter is not null)
        {
            if (string.IsNullOrWhiteSpace(req.DifficultyFilter))
                quiz.DifficultyFilter = null;
            else
            {
                var parsed = QuizQuestionAssembler.ParseDifficulty(req.DifficultyFilter);
                if (parsed is null)
                    return Result<AdminQuizDto>.Failure("Invalid difficulty filter. Use Easy, Medium, or Hard.");
                quiz.DifficultyFilter = parsed;
            }
        }

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

        return Result<AdminQuizDto>.Success(await MapAdminQuizAsync(quiz, ct));
    }

    public async Task<Result<AdminQuizDto>> PublishResultsAsync(Guid topicId, CancellationToken ct = default)
    {
        var quiz = await _db.Quizzes
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.TopicId == topicId && q.Type == QuizType.DailyPracticeTest, ct);

        if (quiz is null)
            return Result<AdminQuizDto>.Failure("Quiz not found.");

        if (quiz.ResultVisibility != ResultVisibilityMode.ManualPublish)
            return Result<AdminQuizDto>.Failure("Results can only be published when visibility is ManualPublish.");

        quiz.ResultsPublishedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Result<AdminQuizDto>.Success(await MapAdminQuizAsync(quiz, ct));
    }

    public async Task<AdminUnitQuizDto?> GetUnitQuizAsync(Guid unitId, string quizType, CancellationToken ct = default)
    {
        if (!TryParseUnitQuizType(quizType, out var type)) return null;

        var quiz = await _db.Quizzes.FirstOrDefaultAsync(q => q.UnitId == unitId && q.Type == type, ct);
        if (quiz is null) return null;

        return await MapAdminUnitQuizAsync(quiz, ct);
    }

    public async Task<Result<AdminUnitQuizDto>> UpdateUnitQuizSettingsAsync(
        Guid unitId, string quizType, UpdateQuizSettingsRequest req, CancellationToken ct = default)
    {
        if (!TryParseUnitQuizType(quizType, out var type))
            return Result<AdminUnitQuizDto>.Failure("Invalid quiz type.");

        if (req.TimeLimitMinutes is < 0)
            return Result<AdminUnitQuizDto>.Failure("Time limit cannot be negative.");

        if (req.AvailableFromUtc is not null && req.AvailableUntilUtc is not null
            && req.AvailableUntilUtc <= req.AvailableFromUtc)
            return Result<AdminUnitQuizDto>.Failure("Available until must be after available from.");

        var quiz = await _db.Quizzes.FirstOrDefaultAsync(q => q.UnitId == unitId && q.Type == type, ct);
        if (quiz is null)
        {
            quiz = new Quiz
            {
                TenantId = _tenant.TenantId,
                UnitId = unitId,
                Type = type,
                Title = type == QuizType.PyqTest ? "PYQ Test" : "Unit Test"
            };
            _db.Quizzes.Add(quiz);
        }

        if (req.ResultVisibility is not null)
        {
            var mode = AssessmentResultPolicy.ParseMode(req.ResultVisibility);
            if (mode is null)
                return Result<AdminUnitQuizDto>.Failure("Invalid result visibility mode.");

            if (mode == ResultVisibilityMode.AfterClose && req.AvailableUntilUtc is null && quiz.AvailableUntilUtc is null)
                return Result<AdminUnitQuizDto>.Failure("After-close visibility requires an end date.");

            if (mode != ResultVisibilityMode.ManualPublish)
                quiz.ResultsPublishedAtUtc = null;

            quiz.ResultVisibility = mode.Value;
        }

        quiz.TimeLimitMinutes = req.TimeLimitMinutes is > 0 ? req.TimeLimitMinutes : null;
        quiz.AvailableFromUtc = req.AvailableFromUtc;
        quiz.AvailableUntilUtc = req.AvailableUntilUtc;

        if (quiz.ResultVisibility == ResultVisibilityMode.AfterClose && quiz.AvailableUntilUtc is null)
            return Result<AdminUnitQuizDto>.Failure("After-close visibility requires an end date.");

        if (req.ShowExplanations is not null)
            quiz.ShowExplanations = req.ShowExplanations.Value;
        if (req.NotifyTeachersOnBatchComplete is not null)
            quiz.NotifyTeachersOnBatchComplete = req.NotifyTeachersOnBatchComplete.Value;
        if (req.BatchCompleteThresholdPercent is not null)
            quiz.BatchCompleteThresholdPercent = Math.Clamp(req.BatchCompleteThresholdPercent.Value, 1, 100);

        if (req.DifficultyFilter is not null)
        {
            if (string.IsNullOrWhiteSpace(req.DifficultyFilter))
                quiz.DifficultyFilter = null;
            else
            {
                var parsed = QuizQuestionAssembler.ParseDifficulty(req.DifficultyFilter);
                if (parsed is null)
                    return Result<AdminUnitQuizDto>.Failure("Invalid difficulty filter. Use Easy, Medium, or Hard.");
                quiz.DifficultyFilter = parsed;
            }
        }

        await _db.SaveChangesAsync(ct);
        return Result<AdminUnitQuizDto>.Success(await MapAdminUnitQuizAsync(quiz, ct));
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

    private static List<string> DeserializeOptions(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private async Task<AdminQuizDto> MapAdminQuizAsync(Quiz quiz, CancellationToken ct)
    {
        var assembled = quiz.IsAssembledQuiz && quiz.UnitId is not null
            ? (await QuizQuestionAssembler.AssembleUnitQuestionsAsync(
                _db, _scope, quiz.UnitId.Value, quiz.Type, null, null, ct)).Count
            : 0;

        return new AdminQuizDto(
            quiz.Id,
            quiz.TopicId,
            quiz.UnitId,
            quiz.Type.ToString(),
            quiz.Title,
            quiz.TimeLimitMinutes,
            quiz.AvailableFromUtc,
            quiz.AvailableUntilUtc,
            quiz.ResultVisibility.ToString(),
            quiz.ShowExplanations,
            quiz.ResultsPublishedAtUtc,
            quiz.NotifyTeachersOnBatchComplete,
            quiz.BatchCompleteThresholdPercent,
            quiz.DifficultyFilter?.ToString(),
            assembled,
            quiz.Questions
                .OrderBy(q => q.Order)
                .Select(ToDto)
                .ToList());
    }

    private async Task<AdminUnitQuizDto> MapAdminUnitQuizAsync(Quiz quiz, CancellationToken ct)
    {
        var count = quiz.UnitId is null
            ? 0
            : (await QuizQuestionAssembler.AssembleUnitQuestionsAsync(
                _db, _scope, quiz.UnitId.Value, quiz.Type, quiz.DifficultyFilter, null, ct)).Count;

        return new AdminUnitQuizDto(
            quiz.Id,
            quiz.UnitId!.Value,
            quiz.Type.ToString(),
            quiz.Title,
            quiz.TimeLimitMinutes,
            quiz.AvailableFromUtc,
            quiz.AvailableUntilUtc,
            quiz.ResultVisibility.ToString(),
            quiz.ShowExplanations,
            quiz.ResultsPublishedAtUtc,
            quiz.NotifyTeachersOnBatchComplete,
            quiz.BatchCompleteThresholdPercent,
            quiz.DifficultyFilter?.ToString(),
            count);
    }

    private static bool TryParseUnitQuizType(string quizType, out QuizType type)
    {
        type = QuizType.UnitTest;
        if (string.IsNullOrWhiteSpace(quizType)) return false;

        var normalized = quizType.Trim().Replace("-", "", StringComparison.OrdinalIgnoreCase);
        return normalized.ToLowerInvariant() switch
        {
            "unittest" => Assign(QuizType.UnitTest, out type),
            "pyqtest" => Assign(QuizType.PyqTest, out type),
            _ => Enum.TryParse(quizType, ignoreCase: true, out type)
                && type is QuizType.UnitTest or QuizType.PyqTest
        };
    }

    private static bool Assign(QuizType value, out QuizType type)
    {
        type = value;
        return true;
    }

    private async Task TryNotifyQuizAvailableAsync(Guid topicId, CancellationToken ct)
    {
        var topicScope = await _scope.GetTopicScopeAsync(topicId, ct);
        if (topicScope is null) return;

        var studentIds = await _enrollments.GetActiveUserIdsForBundleAsync(topicScope.BundleId, ct);
        if (studentIds.Count == 0) return;

        var requests = studentIds.Select(id => new CreateStudentNotificationRequest(
            _tenant.TenantId,
            id,
            "Topic quiz available",
            $"The daily practice quiz for {topicScope.TopicTitle} ({topicScope.SubjectTitle}) is ready.",
            $"/quiz/{topicId}",
            SendEmail: true,
            EmailSubject: $"Quiz ready: {topicScope.TopicTitle}")).ToList();

        await _notifications.NotifyManyAsync(requests, ct);
    }

    private static AdminQuestionDto ToDto(Question q) => new(
        q.Id,
        q.Stem,
        DeserializeOptions(q.OptionsJson),
        q.CorrectKey,
        q.Explanation,
        q.Order,
        q.Difficulty.ToString(),
        q.IsPyq,
        q.PyqYear,
        q.PyqExam);
}
