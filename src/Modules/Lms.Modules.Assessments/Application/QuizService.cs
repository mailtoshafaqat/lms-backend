using System.Text.Json;
using Lms.Modules.Assessments.Contracts;
using Lms.Modules.Assessments.Domain;
using Lms.Modules.Assessments.Infrastructure;
using Lms.Shared.Auth;
using Lms.Shared.Common;
using Lms.Shared.Courses;
using Lms.Shared.Enrollments;
using Lms.Shared.Events;
using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Assessments.Application;

public sealed class QuizService : IQuizService
{
    private readonly AssessmentsDbContext _db;
    private readonly IEventBus _events;
    private readonly ITenantContext _tenant;
    private readonly QuizBatchNotifier _batchNotifier;
    private readonly ICourseScopeReader _scope;
    private readonly IEnrollmentAccessGuard _enrollment;
    private readonly ICurrentUser _currentUser;

    public QuizService(
        AssessmentsDbContext db,
        IEventBus events,
        ITenantContext tenant,
        QuizBatchNotifier batchNotifier,
        ICourseScopeReader scope,
        IEnrollmentAccessGuard enrollment,
        ICurrentUser currentUser)
    {
        _db = db;
        _events = events;
        _tenant = tenant;
        _batchNotifier = batchNotifier;
        _scope = scope;
        _enrollment = enrollment;
        _currentUser = currentUser;
    }

    public Task<QuizDto?> GetByTopicAsync(
        Guid topicId, Guid? userId, string? difficulty = null, CancellationToken ct = default) =>
        GetQuizDtoAsync(
            _db.Quizzes
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.TopicId == topicId && q.Type == QuizType.DailyPracticeTest, ct),
            userId,
            difficulty,
            ct);

    public Task<QuizDto?> GetByUnitAsync(
        Guid unitId, string quizType, Guid? userId, string? difficulty = null, CancellationToken ct = default)
    {
        if (!TryParseUnitQuizType(quizType, out var type))
            return Task.FromResult<QuizDto?>(null);

        return GetQuizDtoAsync(
            _db.Quizzes.FirstOrDefaultAsync(q => q.UnitId == unitId && q.Type == type, ct),
            userId,
            difficulty,
            ct);
    }

    public Task<QuizDto?> GetAsync(
        Guid quizId, Guid? userId, string? difficulty = null, CancellationToken ct = default) =>
        GetQuizDtoAsync(
            _db.Quizzes.Include(q => q.Questions).FirstOrDefaultAsync(q => q.Id == quizId, ct),
            userId,
            difficulty,
            ct);

    public async Task<Result<StartAttemptResultDto>> StartAttemptAsync(
        Guid quizId, Guid userId, CancellationToken ct = default)
    {
        var quiz = await _db.Quizzes
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.Id == quizId, ct);

        if (quiz is null) return Result<StartAttemptResultDto>.Failure("Quiz not found.");

        if (!await CanAccessQuizAsync(quiz, userId, _currentUser.Role, ct))
            return Result<StartAttemptResultDto>.Failure("You are not enrolled in this course.");

        var now = DateTime.UtcNow;
        var status = GetAvailabilityStatus(quiz, now);
        if (status != "Open")
            return Result<StartAttemptResultDto>.Failure(AvailabilityMessage(status, quiz));

        var active = await GetActiveAttemptAsync(quiz.Id, userId, now, ct);
        if (active is not null)
        {
            var resumed = await QuizQuestionAssembler.ResolveAttemptQuestionsAsync(
                _db, _scope, quiz, active, null, ct);
            return Result<StartAttemptResultDto>.Success(new StartAttemptResultDto(
                active.Id,
                active.StartedAt!.Value,
                active.ExpiresAtUtc,
                QuizQuestionAssembler.MapQuestions(resumed),
                QuizQuestionAssembler.DeserializeGuidList(active.FlaggedQuestionIdsJson)));
        }

        if (!quiz.RequiresStartAttempt)
            return Result<StartAttemptResultDto>.Failure("This quiz does not require starting a timed attempt.");

        var questions = await QuizQuestionAssembler.ResolveAttemptQuestionsAsync(
            _db, _scope, quiz, null, null, ct);
        if (questions.Count == 0)
            return Result<StartAttemptResultDto>.Failure("This quiz has no questions yet.");

        var expiresAt = ComputeExpiresAt(quiz, now);
        var attempt = new Attempt
        {
            TenantId = _tenant.TenantId,
            QuizId = quiz.Id,
            UserId = userId,
            StartedAt = now,
            ExpiresAtUtc = expiresAt,
            AnswersJson = "{}",
            QuestionIdsJson = quiz.IsAssembledQuiz
                ? QuizQuestionAssembler.SerializeGuidList(questions.Select(q => q.Id))
                : null,
            FlaggedQuestionIdsJson = "[]"
        };
        _db.Attempts.Add(attempt);
        await _db.SaveChangesAsync(ct);

        return Result<StartAttemptResultDto>.Success(new StartAttemptResultDto(
            attempt.Id,
            attempt.StartedAt!.Value,
            attempt.ExpiresAtUtc,
            QuizQuestionAssembler.MapQuestions(questions),
            Array.Empty<Guid>()));
    }

    public async Task<Result<AttemptResultDto>> SubmitAsync(
        Guid quizId, Guid userId, SubmitAttemptRequest request, CancellationToken ct = default)
    {
        var quiz = await _db.Quizzes
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.Id == quizId, ct);

        if (quiz is null) return Result<AttemptResultDto>.Failure("Quiz not found.");

        if (!await CanAccessQuizAsync(quiz, userId, _currentUser.Role, ct))
            return Result<AttemptResultDto>.Failure("You are not enrolled in this course.");

        var now = DateTime.UtcNow;
        Attempt? attempt = null;

        if (quiz.RequiresStartAttempt)
        {
            if (request.AttemptId is null)
                return Result<AttemptResultDto>.Failure("AttemptId is required for this quiz.");

            attempt = await _db.Attempts.FirstOrDefaultAsync(
                a => a.Id == request.AttemptId && a.QuizId == quizId && a.UserId == userId, ct);

            if (attempt is null) return Result<AttemptResultDto>.Failure("Attempt not found.");
            if (attempt.SubmittedAt is not null)
                return Result<AttemptResultDto>.Failure("This attempt was already submitted.");

            if (attempt.ExpiresAtUtc is not null && now > attempt.ExpiresAtUtc.Value)
                return Result<AttemptResultDto>.Failure("Time is up for this attempt.");

            var status = GetAvailabilityStatus(quiz, now);
            if (status != "Open")
                return Result<AttemptResultDto>.Failure(AvailabilityMessage(status, quiz));
        }

        var questions = await QuizQuestionAssembler.ResolveAttemptQuestionsAsync(
            _db, _scope, quiz, attempt, null, ct);
        if (questions.Count == 0)
            return Result<AttemptResultDto>.Failure("This quiz has no questions yet.");

        var selectedByQuestion = request.Answers.ToDictionary(a => a.QuestionId, a => a.SelectedKey);
        var fullResults = new List<QuestionResultDto>();
        var wrong = new List<Guid>();
        var score = 0;

        foreach (var q in questions)
        {
            selectedByQuestion.TryGetValue(q.Id, out var selected);
            var isCorrect = selected == q.CorrectKey;
            if (isCorrect) score++;
            else wrong.Add(q.Id);

            fullResults.Add(new QuestionResultDto(
                q.Id, q.Stem, DeserializeOptions(q.OptionsJson),
                q.CorrectKey, selected, isCorrect, q.Explanation));
        }

        if (attempt is null)
        {
            attempt = new Attempt
            {
                TenantId = _tenant.TenantId,
                QuizId = quiz.Id,
                UserId = userId,
                StartedAt = now,
                AnswersJson = JsonSerializer.Serialize(selectedByQuestion),
                FlaggedQuestionIdsJson = QuizQuestionAssembler.SerializeGuidList(
                    request.FlaggedQuestionIds ?? Array.Empty<Guid>())
            };
            _db.Attempts.Add(attempt);
        }
        else
        {
            attempt.AnswersJson = JsonSerializer.Serialize(selectedByQuestion);
            attempt.FlaggedQuestionIdsJson = QuizQuestionAssembler.SerializeGuidList(
                request.FlaggedQuestionIds ?? Array.Empty<Guid>());
        }

        attempt.Score = score;
        attempt.Total = questions.Count;
        attempt.SubmittedAt = now;
        await _db.SaveChangesAsync(ct);

        await _events.PublishAsync(new QuizSubmittedEvent(
            attempt.Id, attempt.TenantId, userId, quiz.Id, quiz.TopicId ?? Guid.Empty,
            quiz.Title, score, questions.Count, wrong), ct);

        await _batchNotifier.TryNotifyQuizBatchCompleteAsync(quiz, ct);

        return Result<AttemptResultDto>.Success(BuildAttemptResult(quiz, attempt, fullResults, now));
    }

    public async Task<Result<AttemptResultDto>> GetAttemptResultAsync(
        Guid quizId, Guid userId, CancellationToken ct = default)
    {
        var quiz = await _db.Quizzes
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.Id == quizId, ct);
        if (quiz is null) return Result<AttemptResultDto>.Failure("Quiz not found.");

        if (!await CanAccessQuizAsync(quiz, userId, _currentUser.Role, ct))
            return Result<AttemptResultDto>.Failure("You are not enrolled in this course.");

        var attempt = await _db.Attempts
            .Where(a => a.QuizId == quizId && a.UserId == userId && a.SubmittedAt != null)
            .OrderByDescending(a => a.SubmittedAt)
            .FirstOrDefaultAsync(ct);
        if (attempt is null) return Result<AttemptResultDto>.Failure("No submitted attempt found.");

        var fullResults = await BuildResultsFromAttemptAsync(quiz, attempt, ct);
        return Result<AttemptResultDto>.Success(
            BuildAttemptResult(quiz, attempt, fullResults, DateTime.UtcNow));
    }

    private async Task<QuizDto?> GetQuizDtoAsync(
        Task<Quiz?> quizTask,
        Guid? userId,
        string? difficulty,
        CancellationToken ct)
    {
        var quiz = await quizTask;
        if (quiz is null) return null;

        var role = userId == _currentUser.UserId ? _currentUser.Role : Roles.Student;
        if (!await CanAccessQuizAsync(quiz, userId, role, ct))
            return null;

        return await ToStudentDtoAsync(quiz, userId, difficulty, ct);
    }

    private async Task<bool> CanAccessQuizAsync(
        Quiz quiz, Guid? userId, string? role, CancellationToken ct)
    {
        if (userId is null || role != Roles.Student)
            return true;

        var bundleId = await ResolveBundleIdAsync(quiz, ct);
        if (bundleId is null)
            return true;

        return await _enrollment.HasBundleAccessAsync(userId, role, bundleId.Value, ct);
    }

    private async Task<Guid?> ResolveBundleIdAsync(Quiz quiz, CancellationToken ct)
    {
        if (quiz.TopicId is Guid topicId)
        {
            var scope = await _scope.GetTopicScopeAsync(topicId, ct);
            return scope?.BundleId;
        }

        if (quiz.UnitId is Guid unitId)
            return await _scope.GetBundleIdForUnitAsync(unitId, ct);

        return null;
    }

    private static AttemptResultDto BuildAttemptResult(
        Quiz quiz,
        Attempt attempt,
        IReadOnlyList<QuestionResultDto> fullResults,
        DateTime nowUtc)
    {
        var status = AssessmentResultPolicy.ResolveStatus(
            quiz.ResultVisibility,
            quiz.AvailableUntilUtc,
            quiz.ResultsPublishedAtUtc,
            nowUtc);
        var visible = AssessmentResultPolicy.AreResultsVisible(status);
        var availableAt = AssessmentResultPolicy.ResultsAvailableAtUtc(
            quiz.ResultVisibility,
            quiz.AvailableUntilUtc,
            quiz.ResultsPublishedAtUtc);

        return new AttemptResultDto(
            attempt.Id,
            visible ? attempt.Score : 0,
            attempt.Total > 0 ? attempt.Total : fullResults.Count,
            visible,
            status,
            visible ? null : AssessmentResultPolicy.Message(status, quiz.AvailableUntilUtc, quiz.ResultsPublishedAtUtc),
            availableAt,
            quiz.ShowExplanations,
            AssessmentResultPolicy.GateQuestionResults(visible, quiz.ShowExplanations, fullResults));
    }

    private async Task<List<QuestionResultDto>> BuildResultsFromAttemptAsync(
        Quiz quiz, Attempt attempt, CancellationToken ct)
    {
        var questions = await QuizQuestionAssembler.ResolveAttemptQuestionsAsync(
            _db, _scope, quiz, attempt, null, ct);
        var selectedByQuestion = JsonSerializer.Deserialize<Dictionary<Guid, string>>(attempt.AnswersJson)
            ?? new Dictionary<Guid, string>();
        var results = new List<QuestionResultDto>();

        foreach (var q in questions)
        {
            selectedByQuestion.TryGetValue(q.Id, out var selected);
            var isCorrect = selected == q.CorrectKey;
            results.Add(new QuestionResultDto(
                q.Id, q.Stem, DeserializeOptions(q.OptionsJson),
                q.CorrectKey, selected, isCorrect, q.Explanation));
        }

        return results;
    }

    private async Task<QuizDto> ToStudentDtoAsync(
        Quiz quiz, Guid? userId, string? difficulty, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var status = GetAvailabilityStatus(quiz, now);
        var studentFilter = QuizQuestionAssembler.ParseDifficulty(difficulty);
        ActiveAttemptDto? activeDto = null;
        IReadOnlyList<QuizQuestionDto> questions = Array.Empty<QuizQuestionDto>();
        IReadOnlyList<Guid> flagged = Array.Empty<Guid>();
        IReadOnlyList<string> availableDifficulties = Array.Empty<string>();

        if (quiz.IsAssembledQuiz && quiz.UnitId is not null)
        {
            var pool = await QuizQuestionAssembler.AssembleUnitQuestionsAsync(
                _db, _scope, quiz.UnitId.Value, quiz.Type, quiz.DifficultyFilter, null, ct);
            availableDifficulties = QuizQuestionAssembler.DistinctDifficultyNames(pool);
        }
        else
        {
            availableDifficulties = QuizQuestionAssembler.DistinctDifficultyNames(quiz.Questions);
        }

        if (userId is not null)
        {
            var active = await GetActiveAttemptAsync(quiz.Id, userId.Value, now, ct);
            if (active is not null)
            {
                activeDto = new ActiveAttemptDto(active.Id, active.StartedAt!.Value, active.ExpiresAtUtc);
                flagged = QuizQuestionAssembler.DeserializeGuidList(active.FlaggedQuestionIdsJson);
                var resolved = await QuizQuestionAssembler.ResolveAttemptQuestionsAsync(
                    _db, _scope, quiz, active, studentFilter, ct);
                questions = QuizQuestionAssembler.MapQuestions(resolved);
            }
            else if (!quiz.RequiresStartAttempt && status == "Open")
            {
                var resolved = await QuizQuestionAssembler.ResolveAttemptQuestionsAsync(
                    _db, _scope, quiz, null, studentFilter, ct);
                questions = QuizQuestionAssembler.MapQuestions(resolved);
            }
        }
        else if (!quiz.RequiresStartAttempt && status == "Open")
        {
            var resolved = await QuizQuestionAssembler.ResolveAttemptQuestionsAsync(
                _db, _scope, quiz, null, studentFilter, ct);
            questions = QuizQuestionAssembler.MapQuestions(resolved);
        }

        return new QuizDto(
            quiz.Id,
            quiz.TopicId,
            quiz.UnitId,
            quiz.Type.ToString(),
            quiz.Title,
            quiz.TimeLimitMinutes,
            quiz.AvailableFromUtc,
            quiz.AvailableUntilUtc,
            status,
            quiz.ResultVisibility.ToString(),
            quiz.ShowExplanations,
            quiz.DifficultyFilter?.ToString(),
            availableDifficulties,
            activeDto,
            questions,
            flagged);
    }

    private async Task<Attempt?> GetActiveAttemptAsync(
        Guid quizId, Guid userId, DateTime now, CancellationToken ct)
    {
        var attempt = await _db.Attempts
            .Where(a => a.QuizId == quizId && a.UserId == userId && a.SubmittedAt == null)
            .OrderByDescending(a => a.StartedAt)
            .FirstOrDefaultAsync(ct);

        if (attempt is null) return null;
        if (attempt.ExpiresAtUtc is not null && now > attempt.ExpiresAtUtc.Value)
            return null;

        return attempt;
    }

    private static DateTime? ComputeExpiresAt(Quiz quiz, DateTime startedUtc)
    {
        DateTime? expires = null;

        if (quiz.TimeLimitMinutes is > 0)
            expires = startedUtc.AddMinutes(quiz.TimeLimitMinutes.Value);

        if (quiz.AvailableUntilUtc is not null)
        {
            expires = expires is null
                ? quiz.AvailableUntilUtc
                : expires.Value < quiz.AvailableUntilUtc.Value ? expires : quiz.AvailableUntilUtc;
        }

        return expires;
    }

    private static string GetAvailabilityStatus(Quiz quiz, DateTime nowUtc)
    {
        if (quiz.AvailableFromUtc is not null && nowUtc < quiz.AvailableFromUtc.Value)
            return "NotYetOpen";
        if (quiz.AvailableUntilUtc is not null && nowUtc > quiz.AvailableUntilUtc.Value)
            return "Closed";
        return "Open";
    }

    private static string AvailabilityMessage(string status, Quiz quiz) => status switch
    {
        "NotYetOpen" => quiz.AvailableFromUtc is null
            ? "This test is not open yet."
            : $"This test opens on {quiz.AvailableFromUtc.Value:u}.",
        "Closed" => quiz.AvailableUntilUtc is null
            ? "This test is no longer available."
            : $"This test closed on {quiz.AvailableUntilUtc.Value:u}.",
        _ => "This test is not available."
    };

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
        };
    }

    private static bool Assign(QuizType value, out QuizType type)
    {
        type = value;
        return true;
    }

    private static IReadOnlyList<string> DeserializeOptions(string json) =>
        JsonSerializer.Deserialize<List<string>>(json) ?? [];
}
