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

    private readonly QuizBatchNotifier _batchNotifier;



    public QuizService(

        AssessmentsDbContext db,

        IEventBus events,

        ITenantContext tenant,

        QuizBatchNotifier batchNotifier)

    {

        _db = db;

        _events = events;

        _tenant = tenant;

        _batchNotifier = batchNotifier;

    }



    public async Task<QuizDto?> GetByTopicAsync(Guid topicId, Guid? userId, CancellationToken ct = default)

    {

        var quiz = await _db.Quizzes

            .Include(q => q.Questions)

            .FirstOrDefaultAsync(q => q.TopicId == topicId, ct);

        return quiz is null ? null : await ToStudentDtoAsync(quiz, userId, ct);

    }



    public async Task<QuizDto?> GetAsync(Guid quizId, Guid? userId, CancellationToken ct = default)

    {

        var quiz = await _db.Quizzes

            .Include(q => q.Questions)

            .FirstOrDefaultAsync(q => q.Id == quizId, ct);

        return quiz is null ? null : await ToStudentDtoAsync(quiz, userId, ct);

    }



    public async Task<Result<StartAttemptResultDto>> StartAttemptAsync(

        Guid quizId, Guid userId, CancellationToken ct = default)

    {

        var quiz = await _db.Quizzes

            .Include(q => q.Questions)

            .FirstOrDefaultAsync(q => q.Id == quizId, ct);



        if (quiz is null) return Result<StartAttemptResultDto>.Failure("Quiz not found.");

        if (quiz.Questions.Count == 0)

            return Result<StartAttemptResultDto>.Failure("This quiz has no questions yet.");



        var now = DateTime.UtcNow;

        var status = GetAvailabilityStatus(quiz, now);

        if (status != "Open")

            return Result<StartAttemptResultDto>.Failure(AvailabilityMessage(status, quiz));



        var active = await GetActiveAttemptAsync(quiz.Id, userId, now, ct);

        if (active is not null)

        {

            return Result<StartAttemptResultDto>.Success(new StartAttemptResultDto(

                active.Id,

                active.StartedAt!.Value,

                active.ExpiresAtUtc,

                MapQuestions(quiz)));

        }



        if (!quiz.RequiresScheduledAttempt)

            return Result<StartAttemptResultDto>.Failure("This quiz does not require starting a timed attempt.");



        var expiresAt = ComputeExpiresAt(quiz, now);

        var attempt = new Attempt

        {

            TenantId = _tenant.TenantId,

            QuizId = quiz.Id,

            UserId = userId,

            StartedAt = now,

            ExpiresAtUtc = expiresAt,

            AnswersJson = "{}"

        };

        _db.Attempts.Add(attempt);

        await _db.SaveChangesAsync(ct);



        return Result<StartAttemptResultDto>.Success(new StartAttemptResultDto(

            attempt.Id,

            attempt.StartedAt!.Value,

            attempt.ExpiresAtUtc,

            MapQuestions(quiz)));

    }



    public async Task<Result<AttemptResultDto>> SubmitAsync(

        Guid quizId, Guid userId, SubmitAttemptRequest request, CancellationToken ct = default)

    {

        var quiz = await _db.Quizzes

            .Include(q => q.Questions)

            .FirstOrDefaultAsync(q => q.Id == quizId, ct);



        if (quiz is null) return Result<AttemptResultDto>.Failure("Quiz not found.");



        var now = DateTime.UtcNow;

        Attempt? attempt = null;



        if (quiz.RequiresScheduledAttempt)

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



        var selectedByQuestion = request.Answers.ToDictionary(a => a.QuestionId, a => a.SelectedKey);

        var fullResults = new List<QuestionResultDto>();

        var wrong = new List<Guid>();

        var score = 0;



        foreach (var q in quiz.Questions.OrderBy(x => x.Order))

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

                AnswersJson = JsonSerializer.Serialize(selectedByQuestion)

            };

            _db.Attempts.Add(attempt);

        }

        else

        {

            attempt.AnswersJson = JsonSerializer.Serialize(selectedByQuestion);

        }



        attempt.Score = score;

        attempt.Total = quiz.Questions.Count;

        attempt.SubmittedAt = now;

        await _db.SaveChangesAsync(ct);



        await _events.PublishAsync(new QuizSubmittedEvent(

            attempt.Id, attempt.TenantId, userId, quiz.Id, quiz.TopicId,

            quiz.Title, score, quiz.Questions.Count, wrong), ct);



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



        var attempt = await _db.Attempts

            .Where(a => a.QuizId == quizId && a.UserId == userId && a.SubmittedAt != null)

            .OrderByDescending(a => a.SubmittedAt)

            .FirstOrDefaultAsync(ct);

        if (attempt is null) return Result<AttemptResultDto>.Failure("No submitted attempt found.");



        var fullResults = BuildResultsFromAttempt(quiz, attempt);

        return Result<AttemptResultDto>.Success(

            BuildAttemptResult(quiz, attempt, fullResults, DateTime.UtcNow));

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



    private static List<QuestionResultDto> BuildResultsFromAttempt(Quiz quiz, Attempt attempt)

    {

        var selectedByQuestion = JsonSerializer.Deserialize<Dictionary<Guid, string>>(attempt.AnswersJson)

            ?? new Dictionary<Guid, string>();

        var results = new List<QuestionResultDto>();



        foreach (var q in quiz.Questions.OrderBy(x => x.Order))

        {

            selectedByQuestion.TryGetValue(q.Id, out var selected);

            var isCorrect = selected == q.CorrectKey;

            results.Add(new QuestionResultDto(

                q.Id, q.Stem, DeserializeOptions(q.OptionsJson),

                q.CorrectKey, selected, isCorrect, q.Explanation));

        }



        return results;

    }



    private async Task<QuizDto> ToStudentDtoAsync(Quiz quiz, Guid? userId, CancellationToken ct)

    {

        var now = DateTime.UtcNow;

        var status = GetAvailabilityStatus(quiz, now);

        ActiveAttemptDto? activeDto = null;

        IReadOnlyList<QuizQuestionDto> questions;



        if (userId is not null)

        {

            var active = await GetActiveAttemptAsync(quiz.Id, userId.Value, now, ct);

            if (active is not null)

            {

                activeDto = new ActiveAttemptDto(active.Id, active.StartedAt!.Value, active.ExpiresAtUtc);

                questions = MapQuestions(quiz);

            }

            else if (!quiz.RequiresScheduledAttempt && status == "Open")

            {

                questions = MapQuestions(quiz);

            }

            else

            {

                questions = Array.Empty<QuizQuestionDto>();

            }

        }

        else if (!quiz.RequiresScheduledAttempt && status == "Open")

        {

            questions = MapQuestions(quiz);

        }

        else

        {

            questions = Array.Empty<QuizQuestionDto>();

        }



        return new QuizDto(

            quiz.Id,

            quiz.TopicId,

            quiz.Title,

            quiz.TimeLimitMinutes,

            quiz.AvailableFromUtc,

            quiz.AvailableUntilUtc,

            status,

            quiz.ResultVisibility.ToString(),

            quiz.ShowExplanations,

            activeDto,

            questions);

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



    private static IReadOnlyList<QuizQuestionDto> MapQuestions(Quiz quiz) =>

        quiz.Questions

            .OrderBy(q => q.Order)

            .Select(q => new QuizQuestionDto(q.Id, q.Stem, DeserializeOptions(q.OptionsJson), q.Order))

            .ToList();



    private static IReadOnlyList<string> DeserializeOptions(string json) =>

        JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();

}

