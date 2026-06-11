using System.Text.Json;

using Lms.Modules.Assessments.Domain;

using Lms.Modules.Assessments.Infrastructure;

using Lms.Shared.Common;

using Lms.Shared.Enrollments;

using Lms.Shared.Tenancy;

using Microsoft.EntityFrameworkCore;



namespace Lms.Modules.Assessments.Application;



public sealed class MockExamService : IMockExamService

{

    private readonly AssessmentsDbContext _db;

    private readonly ITenantContext _tenant;

    private readonly IEnrollmentReader _enrollments;

    private readonly QuizBatchNotifier _batchNotifier;



    public MockExamService(

        AssessmentsDbContext db,

        ITenantContext tenant,

        IEnrollmentReader enrollments,

        QuizBatchNotifier batchNotifier)

    {

        _db = db;

        _tenant = tenant;

        _enrollments = enrollments;

        _batchNotifier = batchNotifier;

    }



    public async Task<IReadOnlyList<MockExamSummaryDto>> ListForUserAsync(

        Guid userId, CancellationToken ct = default)

    {

        var activeEnrollments = await _enrollments.GetActiveEnrollmentsAsync(userId, ct);
        if (activeEnrollments.Count == 0) return [];

        var bundleIds = activeEnrollments.Select(e => e.BundleId).ToHashSet();
        var expiresByBundle = activeEnrollments.ToDictionary(e => e.BundleId, e => e.ExpiresAt);

        var exams = await _db.MockExams
            .Include(m => m.Sections).ThenInclude(s => s.Topics)
            .Include(m => m.Topics)
            .Where(m => m.IsPublished && !m.IsArchived && bundleIds.Contains(m.BundleId))
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        var summaries = new List<MockExamSummaryDto>();

        foreach (var exam in exams)
        {
            var active = await GetActiveAttemptAsync(exam.Id, userId, now, ct);
            expiresByBundle.TryGetValue(exam.BundleId, out var expiresAt);
            summaries.Add(await MapSummaryAsync(exam, active, expiresAt, ct));
        }

        return summaries;

    }



    public async Task<MockExamSummaryDto?> GetForUserAsync(

        Guid mockExamId, Guid userId, CancellationToken ct = default)

    {

        var exam = await LoadExamAsync(mockExamId, ct);

        if (exam is null || !exam.IsPublished || exam.IsArchived) return null;

        var activeEnrollments = await _enrollments.GetActiveEnrollmentsAsync(userId, ct);
        var enrollment = activeEnrollments.FirstOrDefault(e => e.BundleId == exam.BundleId);
        if (enrollment is null) return null;

        var active = await GetActiveAttemptAsync(exam.Id, userId, DateTime.UtcNow, ct);
        return await MapSummaryAsync(exam, active, enrollment.ExpiresAt, ct);

    }



    public async Task<Result<StartMockAttemptResultDto>> StartAttemptAsync(

        Guid mockExamId, Guid userId, CancellationToken ct = default)

    {

        var exam = await LoadExamAsync(mockExamId, ct);

        if (exam is null || !exam.IsPublished || exam.IsArchived)
            return Result<StartMockAttemptResultDto>.Failure("Mock exam not found.");

        var bundleIds = await _enrollments.GetActiveBundleIdsAsync(userId, ct);
        if (!bundleIds.Contains(exam.BundleId))
            return Result<StartMockAttemptResultDto>.Failure("You are not enrolled for this exam.");



        var now = DateTime.UtcNow;

        var status = GetAvailabilityStatus(exam, now);

        if (status != "Open")

            return Result<StartMockAttemptResultDto>.Failure(AvailabilityMessage(status, exam));



        var active = await GetActiveAttemptAsync(exam.Id, userId, now, ct);

        if (active is not null)

        {

            var questions = await LoadQuestionsForAttemptAsync(active, exam, ct);
            var sections = BuildSectionNav(exam, questions);

            return Result<StartMockAttemptResultDto>.Success(new StartMockAttemptResultDto(

                active.Id,

                active.StartedAt!.Value,

                active.ExpiresAtUtc,

                questions,

                QuizQuestionAssembler.DeserializeGuidList(active.FlaggedQuestionIdsJson),

                sections));

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

            AnswersJson = "{}",

            FlaggedQuestionIdsJson = "[]"

        };



        _db.MockExamAttempts.Add(attempt);

        await _db.SaveChangesAsync(ct);



        var mapped = await LoadQuestionsByIdsAsync(questionIds, exam, ct);
        var sectionNav = BuildSectionNav(exam, mapped);

        return Result<StartMockAttemptResultDto>.Success(new StartMockAttemptResultDto(

            attempt.Id, attempt.StartedAt!.Value, attempt.ExpiresAtUtc, mapped,
            Array.Empty<Guid>(), sectionNav));

    }



    public async Task<Result<MockExamAttemptResultDto>> SubmitAsync(

        Guid mockExamId, Guid userId, SubmitMockAttemptRequest request, CancellationToken ct = default)

    {

        if (request.AttemptId is null)

            return Result<MockExamAttemptResultDto>.Failure("AttemptId is required.");



        var exam = await LoadExamAsync(mockExamId, ct);

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

        var correct = 0;
        var wrong = 0;



        foreach (var q in questions.OrderBy(q => orderMap.GetValueOrDefault(q.Id, q.Order)))

        {

            selectedByQuestion.TryGetValue(q.Id, out var selected);

            var isCorrect = !string.IsNullOrEmpty(selected) && selected == q.CorrectKey;

            if (!string.IsNullOrEmpty(selected))
            {
                if (isCorrect) correct++;
                else wrong++;
            }



            fullResults.Add(new MockExamQuestionResultDto(

                q.Id, q.Stem, DeserializeOptions(q.OptionsJson),

                q.CorrectKey, string.IsNullOrEmpty(selected) ? null : selected, isCorrect, q.Explanation));

        }



        attempt.AnswersJson = JsonSerializer.Serialize(selectedByQuestion);
        attempt.FlaggedQuestionIdsJson = QuizQuestionAssembler.SerializeGuidList(
            request.FlaggedQuestionIds ?? Array.Empty<Guid>());

        attempt.CorrectCount = correct;
        attempt.WrongCount = wrong;
        attempt.Score = correct * exam.MarksPerCorrect - wrong * exam.PenaltyPerWrong;
        attempt.Total = questions.Count;
        attempt.SubmittedAt = now;

        await _db.SaveChangesAsync(ct);



        await _batchNotifier.TryNotifyMockExamBatchCompleteAsync(exam, ct);



        return Result<MockExamAttemptResultDto>.Success(

            await BuildAttemptResultAsync(exam, attempt, fullResults, now, ct));

    }



    public async Task<Result<MockExamAttemptResultDto>> GetAttemptResultAsync(

        Guid mockExamId, Guid userId, CancellationToken ct = default)

    {

        var exam = await LoadExamAsync(mockExamId, ct);

        if (exam is null) return Result<MockExamAttemptResultDto>.Failure("Mock exam not found.");



        var attempt = await _db.MockExamAttempts

            .Where(a => a.MockExamId == mockExamId && a.UserId == userId && a.SubmittedAt != null)

            .OrderByDescending(a => a.SubmittedAt)

            .FirstOrDefaultAsync(ct);

        if (attempt is null) return Result<MockExamAttemptResultDto>.Failure("No submitted attempt found.");



        var fullResults = await BuildResultsFromAttemptAsync(attempt, ct);

        return Result<MockExamAttemptResultDto>.Success(

            await BuildAttemptResultAsync(exam, attempt, fullResults, DateTime.UtcNow, ct));

    }



    private async Task<MockExamAttemptResultDto> BuildAttemptResultAsync(

        MockExam exam,

        MockExamAttempt attempt,

        IReadOnlyList<MockExamQuestionResultDto> fullResults,

        DateTime nowUtc,

        CancellationToken ct)

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



        MockExamRankDto? rank = null;
        if (visible)
        {
            var ranked = MockExamRankCalculator.OrderForRanking(
                await _db.MockExamAttempts
                    .Where(a => a.MockExamId == exam.Id && a.SubmittedAt != null)
                    .ToListAsync(ct));
            rank = MockExamRankCalculator.ComputeRank(attempt, ranked);
        }



        return new MockExamAttemptResultDto(

            attempt.Id,

            visible ? attempt.Score : 0,

            attempt.Total > 0 ? attempt.Total : fullResults.Count,

            visible ? attempt.CorrectCount : 0,

            visible ? attempt.WrongCount : 0,

            exam.MarksPerCorrect,

            exam.PenaltyPerWrong,

            visible,

            status,

            visible ? null : AssessmentResultPolicy.Message(status, exam.AvailableUntilUtc, exam.ResultsPublishedAtUtc),

            availableAt,

            exam.ShowExplanations,

            rank,

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

            var isCorrect = !string.IsNullOrEmpty(selected) && selected == q.CorrectKey;

            results.Add(new MockExamQuestionResultDto(

                q.Id, q.Stem, DeserializeOptions(q.OptionsJson),

                q.CorrectKey, string.IsNullOrEmpty(selected) ? null : selected, isCorrect, q.Explanation));

        }



        return results;

    }



    private async Task<MockExamSummaryDto> MapSummaryAsync(
        MockExam exam, MockExamAttempt? active, DateTime? accessExpiresAt, CancellationToken ct)

    {

        var totalQuestions = 0;

        foreach (var topic in GetOrderedTopics(exam))

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
            exam.BundleId,
            exam.BundleTitle,
            exam.SubjectId,
            exam.SubjectTitle,
            exam.Title,
            exam.Description,
            exam.TimeLimitMinutes,
            exam.MarksPerCorrect,
            exam.PenaltyPerWrong,
            exam.AvailableFromUtc,
            exam.AvailableUntilUtc,
            GetAvailabilityStatus(exam, now),
            totalQuestions,
            accessExpiresAt,
            activeDto);

    }



    private async Task<List<Guid>> AssembleQuestionIdsAsync(MockExam exam, CancellationToken ct)

    {

        var ids = new List<Guid>();

        foreach (var section in exam.Sections.OrderBy(s => s.SortOrder))

        {

            foreach (var topic in section.Topics.OrderBy(t => t.Order))

            {

                var quiz = await _db.Quizzes.Include(q => q.Questions)

                    .FirstOrDefaultAsync(q => q.TopicId == topic.TopicId, ct);

                if (quiz is null) continue;



                var pool = quiz.Questions.OrderBy(q => q.Order).Select(q => q.Id).ToList();

                var take = topic.QuestionCount == 0 ? pool.Count : topic.QuestionCount;

                ids.AddRange(pool.Take(take));

            }

        }



        return ids;

    }



    private static IReadOnlyList<MockExamTopic> GetOrderedTopics(MockExam exam) =>
        exam.Sections.Count > 0
            ? exam.Sections.OrderBy(s => s.SortOrder).SelectMany(s => s.Topics.OrderBy(t => t.Order)).ToList()
            : exam.Topics.OrderBy(t => t.Order).ToList();



    private async Task<IReadOnlyList<MockExamQuestionDto>> LoadQuestionsForAttemptAsync(

        MockExamAttempt attempt, MockExam exam, CancellationToken ct)

    {

        var ids = JsonSerializer.Deserialize<List<Guid>>(attempt.QuestionIdsJson) ?? [];

        return await LoadQuestionsByIdsAsync(ids, exam, ct);

    }



    private async Task<IReadOnlyList<MockExamQuestionDto>> LoadQuestionsByIdsAsync(

        List<Guid> ids, MockExam exam, CancellationToken ct)

    {

        var questions = await _db.Questions.Where(q => ids.Contains(q.Id)).ToListAsync(ct);

        var orderMap = ids.Select((id, i) => (id, i)).ToDictionary(x => x.id, x => x.i);

        var sectionByQuestion = BuildQuestionSectionMap(exam, ids);



        return questions

            .OrderBy(q => orderMap.GetValueOrDefault(q.Id, q.Order))

            .Select((q, i) => new MockExamQuestionDto(
                q.Id,
                q.Stem,
                DeserializeOptions(q.OptionsJson),
                i + 1,
                sectionByQuestion.GetValueOrDefault(q.Id)))

            .ToList();

    }



    private static Dictionary<Guid, string> BuildQuestionSectionMap(MockExam exam, IReadOnlyList<Guid> assembledIds)
    {
        var map = new Dictionary<Guid, string>();
        var index = 0;

        foreach (var section in exam.Sections.OrderBy(s => s.SortOrder))
        {
            foreach (var topic in section.Topics.OrderBy(t => t.Order))
            {
                var take = topic.QuestionCount == 0 ? int.MaxValue : topic.QuestionCount;
                var count = 0;
                while (index < assembledIds.Count && count < take)
                {
                    map.TryAdd(assembledIds[index], section.Title);
                    index++;
                    count++;
                }
            }
        }

        return map;
    }



    private static IReadOnlyList<MockExamSectionNavDto> BuildSectionNav(
        MockExam exam, IReadOnlyList<MockExamQuestionDto> questions)
    {
        if (exam.Sections.Count == 0 || questions.Count == 0) return [];

        var nav = new List<MockExamSectionNavDto>();
        var grouped = questions
            .GroupBy(q => q.SectionTitle ?? "General")
            .ToList();

        var order = 1;
        var start = 1;
        foreach (var section in exam.Sections.OrderBy(s => s.SortOrder))
        {
            var count = grouped.FirstOrDefault(g => g.Key == section.Title)?.Count() ?? 0;
            if (count > 0)
            {
                nav.Add(new MockExamSectionNavDto(section.Title, order, start, count));
                start += count;
            }
            order++;
        }

        return nav;
    }



    private Task<MockExam?> LoadExamAsync(Guid id, CancellationToken ct) =>
        _db.MockExams
            .Include(m => m.Sections).ThenInclude(s => s.Topics)
            .Include(m => m.Topics)
            .FirstOrDefaultAsync(m => m.Id == id, ct);



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
