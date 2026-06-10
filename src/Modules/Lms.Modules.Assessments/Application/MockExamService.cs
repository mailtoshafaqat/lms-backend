using System.Text.Json;

using Lms.Modules.Assessments.Domain;

using Lms.Modules.Assessments.Infrastructure;

using Lms.Shared.Common;

using Lms.Shared.Courses;

using Lms.Shared.Enrollments;

using Lms.Shared.Tenancy;

using Microsoft.EntityFrameworkCore;



namespace Lms.Modules.Assessments.Application;



public sealed class MockExamService : IMockExamService

{

    private readonly AssessmentsDbContext _db;

    private readonly ITenantContext _tenant;

    private readonly IEnrollmentReader _enrollments;

    private readonly ICourseScopeReader _scope;

    private readonly QuizBatchNotifier _batchNotifier;



    public MockExamService(

        AssessmentsDbContext db,

        ITenantContext tenant,

        IEnrollmentReader enrollments,

        ICourseScopeReader scope,

        QuizBatchNotifier batchNotifier)

    {

        _db = db;

        _tenant = tenant;

        _enrollments = enrollments;

        _scope = scope;

        _batchNotifier = batchNotifier;

    }



    public async Task<IReadOnlyList<MockExamSummaryDto>> ListForUserAsync(

        Guid userId, CancellationToken ct = default)

    {

        var bundleIds = await _enrollments.GetActiveBundleIdsAsync(userId, ct);

        if (bundleIds.Count == 0) return [];



        var exams = await _db.MockExams

            .Include(m => m.Topics)

            .Where(m => m.IsPublished)

            .OrderByDescending(m => m.CreatedAt)

            .ToListAsync(ct);



        var now = DateTime.UtcNow;

        var summaries = new List<MockExamSummaryDto>();



        foreach (var exam in exams)

        {

            if (!await IsUserEligibleAsync(exam.SubjectId, bundleIds, ct)) continue;



            var active = await GetActiveAttemptAsync(exam.Id, userId, now, ct);

            summaries.Add(await MapSummaryAsync(exam, active, ct));

        }



        return summaries;

    }



    public async Task<MockExamSummaryDto?> GetForUserAsync(

        Guid mockExamId, Guid userId, CancellationToken ct = default)

    {

        var exam = await _db.MockExams.Include(m => m.Topics).FirstOrDefaultAsync(m => m.Id == mockExamId, ct);

        if (exam is null || !exam.IsPublished) return null;



        var bundleIds = await _enrollments.GetActiveBundleIdsAsync(userId, ct);

        if (!await IsUserEligibleAsync(exam.SubjectId, bundleIds, ct)) return null;



        var active = await GetActiveAttemptAsync(exam.Id, userId, DateTime.UtcNow, ct);

        return await MapSummaryAsync(exam, active, ct);

    }



    public async Task<Result<StartMockAttemptResultDto>> StartAttemptAsync(

        Guid mockExamId, Guid userId, CancellationToken ct = default)

    {

        var exam = await _db.MockExams.Include(m => m.Topics).FirstOrDefaultAsync(m => m.Id == mockExamId, ct);

        if (exam is null || !exam.IsPublished)

            return Result<StartMockAttemptResultDto>.Failure("Mock exam not found.");



        var bundleIds = await _enrollments.GetActiveBundleIdsAsync(userId, ct);

        if (!await IsUserEligibleAsync(exam.SubjectId, bundleIds, ct))

            return Result<StartMockAttemptResultDto>.Failure("You are not enrolled for this exam.");



        var now = DateTime.UtcNow;

        var status = GetAvailabilityStatus(exam, now);

        if (status != "Open")

            return Result<StartMockAttemptResultDto>.Failure(AvailabilityMessage(status, exam));



        var active = await GetActiveAttemptAsync(exam.Id, userId, now, ct);

        if (active is not null)

        {

            var questions = await LoadQuestionsForAttemptAsync(active, ct);

            return Result<StartMockAttemptResultDto>.Success(new StartMockAttemptResultDto(

                active.Id,

                active.StartedAt!.Value,

                active.ExpiresAtUtc,

                questions));

        }



        var questionIds = await AssembleQuestionIdsAsync(exam, ct);

        if (questionIds.Count == 0)

            return Result<StartMockAttemptResultDto>.Failure("This mock exam has no questions yet.");



        var expiresAt = ComputeExpiresAt(exam, now);

        var attempt = new MockExamAttempt

        {

            TenantId = _tenant.TenantId,

            MockExamId = exam.Id,

            UserId = userId,

            StartedAt = now,

            ExpiresAtUtc = expiresAt,

            QuestionIdsJson = JsonSerializer.Serialize(questionIds),

            AnswersJson = "{}"

        };



        _db.MockExamAttempts.Add(attempt);

        await _db.SaveChangesAsync(ct);



        var mapped = await LoadQuestionsByIdsAsync(questionIds, ct);

        return Result<StartMockAttemptResultDto>.Success(new StartMockAttemptResultDto(

            attempt.Id, attempt.StartedAt!.Value, attempt.ExpiresAtUtc, mapped));

    }



    public async Task<Result<MockExamAttemptResultDto>> SubmitAsync(

        Guid mockExamId, Guid userId, SubmitMockAttemptRequest request, CancellationToken ct = default)

    {

        if (request.AttemptId is null)

            return Result<MockExamAttemptResultDto>.Failure("AttemptId is required.");



        var exam = await _db.MockExams.FirstOrDefaultAsync(m => m.Id == mockExamId, ct);

        if (exam is null) return Result<MockExamAttemptResultDto>.Failure("Mock exam not found.");



        var attempt = await _db.MockExamAttempts.FirstOrDefaultAsync(

            a => a.Id == request.AttemptId && a.MockExamId == mockExamId && a.UserId == userId, ct);

        if (attempt is null) return Result<MockExamAttemptResultDto>.Failure("Attempt not found.");

        if (attempt.SubmittedAt is not null)

            return Result<MockExamAttemptResultDto>.Failure("This attempt was already submitted.");



        var now = DateTime.UtcNow;

        if (attempt.ExpiresAtUtc is not null && now > attempt.ExpiresAtUtc.Value)

            return Result<MockExamAttemptResultDto>.Failure("Time is up for this attempt.");



        var questionIds = JsonSerializer.Deserialize<List<Guid>>(attempt.QuestionIdsJson) ?? [];

        var questions = await _db.Questions.Where(q => questionIds.Contains(q.Id)).ToListAsync(ct);

        var orderMap = questionIds.Select((id, i) => (id, i)).ToDictionary(x => x.id, x => x.i);



        var selectedByQuestion = request.Answers.ToDictionary(a => a.QuestionId, a => a.SelectedKey);

        var fullResults = new List<MockExamQuestionResultDto>();

        var score = 0;



        foreach (var q in questions.OrderBy(q => orderMap.GetValueOrDefault(q.Id, q.Order)))

        {

            selectedByQuestion.TryGetValue(q.Id, out var selected);

            var isCorrect = selected == q.CorrectKey;

            if (isCorrect) score++;



            fullResults.Add(new MockExamQuestionResultDto(

                q.Id, q.Stem, DeserializeOptions(q.OptionsJson),

                q.CorrectKey, selected, isCorrect, q.Explanation));

        }



        attempt.AnswersJson = JsonSerializer.Serialize(selectedByQuestion);

        attempt.Score = score;

        attempt.Total = questions.Count;

        attempt.SubmittedAt = now;

        await _db.SaveChangesAsync(ct);



        await _batchNotifier.TryNotifyMockExamBatchCompleteAsync(exam, ct);



        return Result<MockExamAttemptResultDto>.Success(

            BuildAttemptResult(exam, attempt, fullResults, now));

    }



    public async Task<Result<MockExamAttemptResultDto>> GetAttemptResultAsync(

        Guid mockExamId, Guid userId, CancellationToken ct = default)

    {

        var exam = await _db.MockExams.FirstOrDefaultAsync(m => m.Id == mockExamId, ct);

        if (exam is null) return Result<MockExamAttemptResultDto>.Failure("Mock exam not found.");



        var attempt = await _db.MockExamAttempts

            .Where(a => a.MockExamId == mockExamId && a.UserId == userId && a.SubmittedAt != null)

            .OrderByDescending(a => a.SubmittedAt)

            .FirstOrDefaultAsync(ct);

        if (attempt is null) return Result<MockExamAttemptResultDto>.Failure("No submitted attempt found.");



        var fullResults = await BuildResultsFromAttemptAsync(attempt, ct);

        return Result<MockExamAttemptResultDto>.Success(

            BuildAttemptResult(exam, attempt, fullResults, DateTime.UtcNow));

    }



    private static MockExamAttemptResultDto BuildAttemptResult(

        MockExam exam,

        MockExamAttempt attempt,

        IReadOnlyList<MockExamQuestionResultDto> fullResults,

        DateTime nowUtc)

    {

        var status = AssessmentResultPolicy.ResolveStatus(

            exam.ResultVisibility,

            exam.AvailableUntilUtc,

            exam.ResultsPublishedAtUtc,

            nowUtc);

        var visible = AssessmentResultPolicy.AreResultsVisible(status);

        var availableAt = AssessmentResultPolicy.ResultsAvailableAtUtc(

            exam.ResultVisibility,

            exam.AvailableUntilUtc,

            exam.ResultsPublishedAtUtc);



        return new MockExamAttemptResultDto(

            attempt.Id,

            visible ? attempt.Score : 0,
            attempt.Total > 0 ? attempt.Total : fullResults.Count,

            visible,

            status,

            visible ? null : AssessmentResultPolicy.Message(status, exam.AvailableUntilUtc, exam.ResultsPublishedAtUtc),

            availableAt,

            exam.ShowExplanations,

            AssessmentResultPolicy.GateMockQuestionResults(visible, exam.ShowExplanations, fullResults));

    }



    private async Task<List<MockExamQuestionResultDto>> BuildResultsFromAttemptAsync(

        MockExamAttempt attempt, CancellationToken ct)

    {

        var questionIds = JsonSerializer.Deserialize<List<Guid>>(attempt.QuestionIdsJson) ?? [];

        var questions = await _db.Questions.Where(q => questionIds.Contains(q.Id)).ToListAsync(ct);

        var orderMap = questionIds.Select((id, i) => (id, i)).ToDictionary(x => x.id, x => x.i);

        var selectedByQuestion = JsonSerializer.Deserialize<Dictionary<Guid, string>>(attempt.AnswersJson)

            ?? new Dictionary<Guid, string>();

        var results = new List<MockExamQuestionResultDto>();



        foreach (var q in questions.OrderBy(q => orderMap.GetValueOrDefault(q.Id, q.Order)))

        {

            selectedByQuestion.TryGetValue(q.Id, out var selected);

            var isCorrect = selected == q.CorrectKey;

            results.Add(new MockExamQuestionResultDto(

                q.Id, q.Stem, DeserializeOptions(q.OptionsJson),

                q.CorrectKey, selected, isCorrect, q.Explanation));

        }



        return results;

    }



    private async Task<bool> IsUserEligibleAsync(

        Guid subjectId, IReadOnlyList<Guid> bundleIds, CancellationToken ct)

    {

        if (bundleIds.Count == 0) return false;

        var subject = await _scope.GetSubjectScopeAsync(subjectId, ct);

        return subject is not null && bundleIds.Contains(subject.BundleId);

    }



    private async Task<MockExamSummaryDto> MapSummaryAsync(

        MockExam exam, MockExamAttempt? active, CancellationToken ct)

    {

        var totalQuestions = 0;

        foreach (var topic in exam.Topics.OrderBy(t => t.Order))

        {

            var count = await _db.Quizzes.Where(q => q.TopicId == topic.TopicId)

                .SelectMany(q => q.Questions)

                .CountAsync(ct);

            totalQuestions += topic.QuestionCount == 0 ? count : topic.QuestionCount;

        }



        var now = DateTime.UtcNow;

        ActiveMockAttemptDto? activeDto = null;

        if (active is not null)

            activeDto = new ActiveMockAttemptDto(active.Id, active.StartedAt!.Value, active.ExpiresAtUtc);



        return new MockExamSummaryDto(

            exam.Id,

            exam.SubjectId,

            exam.SubjectTitle,

            exam.Title,

            exam.Description,

            exam.TimeLimitMinutes,

            exam.AvailableFromUtc,

            exam.AvailableUntilUtc,

            GetAvailabilityStatus(exam, now),

            totalQuestions,

            activeDto);

    }



    private async Task<List<Guid>> AssembleQuestionIdsAsync(MockExam exam, CancellationToken ct)

    {

        var ids = new List<Guid>();

        foreach (var topic in exam.Topics.OrderBy(t => t.Order))

        {

            var quiz = await _db.Quizzes.Include(q => q.Questions)

                .FirstOrDefaultAsync(q => q.TopicId == topic.TopicId, ct);

            if (quiz is null) continue;



            var pool = quiz.Questions.OrderBy(q => q.Order).Select(q => q.Id).ToList();

            var take = topic.QuestionCount == 0 ? pool.Count : topic.QuestionCount;

            ids.AddRange(pool.Take(take));

        }



        return ids;

    }



    private async Task<IReadOnlyList<MockExamQuestionDto>> LoadQuestionsForAttemptAsync(

        MockExamAttempt attempt, CancellationToken ct)

    {

        var ids = JsonSerializer.Deserialize<List<Guid>>(attempt.QuestionIdsJson) ?? [];

        return await LoadQuestionsByIdsAsync(ids, ct);

    }



    private async Task<IReadOnlyList<MockExamQuestionDto>> LoadQuestionsByIdsAsync(

        List<Guid> ids, CancellationToken ct)

    {

        var questions = await _db.Questions.Where(q => ids.Contains(q.Id)).ToListAsync(ct);

        var orderMap = ids.Select((id, i) => (id, i)).ToDictionary(x => x.id, x => x.i);



        return questions

            .OrderBy(q => orderMap.GetValueOrDefault(q.Id, q.Order))

            .Select((q, i) => new MockExamQuestionDto(q.Id, q.Stem, DeserializeOptions(q.OptionsJson), i + 1))

            .ToList();

    }



    private async Task<MockExamAttempt?> GetActiveAttemptAsync(

        Guid mockExamId, Guid userId, DateTime now, CancellationToken ct)

    {

        var attempt = await _db.MockExamAttempts

            .Where(a => a.MockExamId == mockExamId && a.UserId == userId && a.SubmittedAt == null)

            .OrderByDescending(a => a.StartedAt)

            .FirstOrDefaultAsync(ct);



        if (attempt is null) return null;

        if (attempt.ExpiresAtUtc is not null && now > attempt.ExpiresAtUtc.Value)

            return null;



        return attempt;

    }



    private static DateTime? ComputeExpiresAt(MockExam exam, DateTime startedUtc) =>

        startedUtc.AddMinutes(exam.TimeLimitMinutes);



    private static string GetAvailabilityStatus(MockExam exam, DateTime nowUtc)

    {

        if (exam.AvailableFromUtc is not null && nowUtc < exam.AvailableFromUtc.Value)

            return "NotYetOpen";

        if (exam.AvailableUntilUtc is not null && nowUtc > exam.AvailableUntilUtc.Value)

            return "Closed";

        return "Open";

    }



    private static string AvailabilityMessage(string status, MockExam exam) => status switch

    {

        "NotYetOpen" => exam.AvailableFromUtc is null

            ? "This exam is not open yet."

            : $"This exam opens on {exam.AvailableFromUtc.Value:u}.",

        "Closed" => exam.AvailableUntilUtc is null

            ? "This exam is no longer available."

            : $"This exam closed on {exam.AvailableUntilUtc.Value:u}.",

        _ => "This exam is not available."

    };



    private static IReadOnlyList<string> DeserializeOptions(string json) =>

        JsonSerializer.Deserialize<List<string>>(json) ?? [];

}

