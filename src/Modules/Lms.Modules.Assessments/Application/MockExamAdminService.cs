using Lms.Modules.Assessments.Domain;
using Lms.Modules.Assessments.Infrastructure;
using Lms.Shared.Common;
using Lms.Shared.Courses;
using Lms.Shared.Tenancy;
using Lms.Shared.Users;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Assessments.Application;

public sealed class MockExamAdminService : IMockExamAdminService
{
    private readonly AssessmentsDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICourseScopeReader _scope;
    private readonly IUserDirectory _users;

    public MockExamAdminService(
        AssessmentsDbContext db,
        ITenantContext tenant,
        ICourseScopeReader scope,
        IUserDirectory users)
    {
        _db = db;
        _tenant = tenant;
        _scope = scope;
        _users = users;
    }

    public async Task<IReadOnlyList<AdminMockExamDto>> ListForSubjectAsync(
        Guid subjectId, bool includeArchived = false, CancellationToken ct = default)
    {
        var query = _db.MockExams
            .Include(m => m.Sections).ThenInclude(s => s.Topics)
            .Include(m => m.Topics)
            .Where(m => m.SubjectId == subjectId);

        if (!includeArchived)
            query = query.Where(m => !m.IsArchived);

        var rows = await query.OrderByDescending(m => m.CreatedAt).ToListAsync(ct);
        return rows.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<AdminMockExamDto>> ListForBundleAsync(
        Guid bundleId, bool includeArchived = false, CancellationToken ct = default)
    {
        var query = _db.MockExams
            .Include(m => m.Sections).ThenInclude(s => s.Topics)
            .Include(m => m.Topics)
            .Where(m => m.BundleId == bundleId);

        if (!includeArchived)
            query = query.Where(m => !m.IsArchived);

        var rows = await query.OrderByDescending(m => m.CreatedAt).ToListAsync(ct);
        return rows.Select(Map).ToList();
    }

    public async Task<AdminMockExamDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var row = await LoadExamAsync(id, ct);
        return row is null ? null : Map(row);
    }

    public async Task<Result<AdminMockExamDto>> CreateAsync(
        CreateMockExamRequest request, CancellationToken ct = default)
    {
        var sections = NormalizeSections(request.Sections, request.Topics);
        var validation = await ValidateRequestAsync(request.SubjectId, request.Title, request.TimeLimitMinutes,
            request.MarksPerCorrect, request.PenaltyPerWrong,
            request.AvailableFromUtc, request.AvailableUntilUtc, sections, ct);
        if (!validation.Succeeded) return Result<AdminMockExamDto>.Failure(validation.Error!);

        var scope = await _scope.GetSubjectScopeAsync(request.SubjectId, ct)!;
        var topicTitles = validation.Value!;

        var entity = new MockExam
        {
            TenantId = _tenant.TenantId,
            BundleId = scope!.BundleId,
            BundleTitle = scope.BundleTitle,
            SubjectId = request.SubjectId,
            SubjectTitle = scope.SubjectTitle,
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            TimeLimitMinutes = request.TimeLimitMinutes,
            MarksPerCorrect = request.MarksPerCorrect ?? 1m,
            PenaltyPerWrong = request.PenaltyPerWrong ?? 0m,
            AvailableFromUtc = request.AvailableFromUtc,
            AvailableUntilUtc = request.AvailableUntilUtc,
            IsPublished = request.IsPublished,
            ResultVisibility = ParseVisibility(request.ResultVisibility),
            ShowExplanations = request.ShowExplanations ?? true,
            NotifyTeachersOnBatchComplete = request.NotifyTeachersOnBatchComplete ?? true,
            BatchCompleteThresholdPercent = Math.Clamp(request.BatchCompleteThresholdPercent ?? 80, 1, 100)
        };

        if (entity.ResultVisibility == ResultVisibilityMode.AfterClose && entity.AvailableUntilUtc is null)
            return Result<AdminMockExamDto>.Failure("After-close visibility requires an end date.");

        ApplySections(entity, sections, topicTitles);

        _db.MockExams.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Result<AdminMockExamDto>.Success(Map(entity));
    }

    public async Task<Result<AdminMockExamDto>> UpdateAsync(
        Guid id, UpdateMockExamRequest request, CancellationToken ct = default)
    {
        var entity = await LoadExamAsync(id, ct);
        if (entity is null) return Result<AdminMockExamDto>.Failure("Mock exam not found.");

        var sections = NormalizeSections(request.Sections, request.Topics);
        var validation = await ValidateRequestAsync(entity.SubjectId, request.Title, request.TimeLimitMinutes,
            request.MarksPerCorrect, request.PenaltyPerWrong,
            request.AvailableFromUtc, request.AvailableUntilUtc, sections, ct);
        if (!validation.Succeeded) return Result<AdminMockExamDto>.Failure(validation.Error!);

        var topicTitles = validation.Value!;
        ApplyPolicyFields(entity, request.ResultVisibility, request.ShowExplanations,
            request.NotifyTeachersOnBatchComplete, request.BatchCompleteThresholdPercent,
            request.AvailableUntilUtc);
        entity.Title = request.Title.Trim();
        entity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        entity.TimeLimitMinutes = request.TimeLimitMinutes;
        if (request.MarksPerCorrect is not null) entity.MarksPerCorrect = request.MarksPerCorrect.Value;
        if (request.PenaltyPerWrong is not null) entity.PenaltyPerWrong = request.PenaltyPerWrong.Value;
        entity.AvailableFromUtc = request.AvailableFromUtc;
        entity.AvailableUntilUtc = request.AvailableUntilUtc;
        entity.IsPublished = request.IsPublished;

        if (entity.ResultVisibility == ResultVisibilityMode.AfterClose && entity.AvailableUntilUtc is null)
            return Result<AdminMockExamDto>.Failure("After-close visibility requires an end date.");

        _db.MockExamTopics.RemoveRange(entity.Topics);
        _db.MockExamSections.RemoveRange(entity.Sections);
        entity.Topics.Clear();
        entity.Sections.Clear();

        ApplySections(entity, sections, topicTitles);

        await _db.SaveChangesAsync(ct);
        return Result<AdminMockExamDto>.Success(Map(entity));
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await LoadExamAsync(id, ct);
        if (entity is null) return false;

        _db.MockExamTopics.RemoveRange(entity.Topics);
        _db.MockExamSections.RemoveRange(entity.Sections);
        _db.MockExams.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<Result<AdminMockExamDto>> PublishResultsAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await LoadExamAsync(id, ct);
        if (entity is null) return Result<AdminMockExamDto>.Failure("Mock exam not found.");

        if (entity.ResultVisibility != ResultVisibilityMode.ManualPublish)
            return Result<AdminMockExamDto>.Failure("Results can only be published when visibility is ManualPublish.");

        entity.ResultsPublishedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Result<AdminMockExamDto>.Success(Map(entity));
    }

    public async Task<Result<AdminMockExamDto>> SetArchivedAsync(
        Guid id, bool isArchived, CancellationToken ct = default)
    {
        var entity = await LoadExamAsync(id, ct);
        if (entity is null) return Result<AdminMockExamDto>.Failure("Mock exam not found.");

        entity.IsArchived = isArchived;
        await _db.SaveChangesAsync(ct);

        return Result<AdminMockExamDto>.Success(Map(entity));
    }

    public async Task<Result<MockExamLeaderboardDto>> GetLeaderboardAsync(
        Guid mockExamId, Guid? currentUserId, int take = 100, CancellationToken ct = default)
    {
        var exam = await _db.MockExams.FirstOrDefaultAsync(m => m.Id == mockExamId, ct);
        if (exam is null) return Result<MockExamLeaderboardDto>.Failure("Mock exam not found.");

        var ranked = MockExamRankCalculator.OrderForRanking(
            await _db.MockExamAttempts
                .Where(a => a.MockExamId == mockExamId && a.SubmittedAt != null)
                .ToListAsync(ct));

        var slice = ranked.Take(Math.Clamp(take, 1, 500)).ToList();
        var names = await _users.GetDisplayNamesAsync(slice.Select(a => a.UserId), ct);

        var rows = slice.Select((attempt, i) => new MockExamLeaderboardRowDto(
            i + 1,
            attempt.UserId,
            names.GetValueOrDefault(attempt.UserId, "Student"),
            attempt.Score,
            attempt.CorrectCount,
            attempt.WrongCount,
            attempt.SubmittedAt!.Value,
            currentUserId == attempt.UserId)).ToList();

        return Result<MockExamLeaderboardDto>.Success(new MockExamLeaderboardDto(rows, ranked.Count));
    }

    private static void ApplySections(
        MockExam entity,
        IReadOnlyList<MockExamSectionInput> sections,
        Dictionary<Guid, string> topicTitles)
    {
        foreach (var sectionInput in sections.OrderBy(s => s.SortOrder))
        {
            var section = new MockExamSection
            {
                TenantId = entity.TenantId,
                Title = sectionInput.Title.Trim(),
                SortOrder = sectionInput.SortOrder,
                SectionTimeLimitMinutes = sectionInput.SectionTimeLimitMinutes
            };

            var topicOrder = 1;
            foreach (var topic in sectionInput.Topics)
            {
                var row = new MockExamTopic
                {
                    TenantId = entity.TenantId,
                    TopicId = topic.TopicId,
                    TopicTitle = topicTitles[topic.TopicId],
                    QuestionCount = topic.QuestionCount,
                    Order = topicOrder++
                };
                section.Topics.Add(row);
                entity.Topics.Add(row);
            }

            entity.Sections.Add(section);
        }
    }

    private static IReadOnlyList<MockExamSectionInput> NormalizeSections(
        IReadOnlyList<MockExamSectionInput>? sections,
        IReadOnlyList<MockExamTopicInput>? legacyTopics)
    {
        if (sections is { Count: > 0 }) return sections;
        if (legacyTopics is { Count: > 0 })
        {
            return
            [
                new MockExamSectionInput("General", 1, null, legacyTopics)
            ];
        }

        return [];
    }

    private static void ApplyPolicyFields(
        MockExam entity,
        string? resultVisibility,
        bool? showExplanations,
        bool? notifyTeachers,
        int? batchThreshold,
        DateTime? availableUntilUtc)
    {
        if (resultVisibility is not null)
        {
            var mode = AssessmentResultPolicy.ParseMode(resultVisibility);
            if (mode is not null)
            {
                if (mode != ResultVisibilityMode.ManualPublish)
                    entity.ResultsPublishedAtUtc = null;
                entity.ResultVisibility = mode.Value;
            }
        }

        if (showExplanations is not null)
            entity.ShowExplanations = showExplanations.Value;
        if (notifyTeachers is not null)
            entity.NotifyTeachersOnBatchComplete = notifyTeachers.Value;
        if (batchThreshold is not null)
            entity.BatchCompleteThresholdPercent = Math.Clamp(batchThreshold.Value, 1, 100);
    }

    private static ResultVisibilityMode ParseVisibility(string? value) =>
        AssessmentResultPolicy.ParseMode(value) ?? ResultVisibilityMode.AfterClose;

    private async Task<Result<Dictionary<Guid, string>>> ValidateRequestAsync(
        Guid subjectId,
        string title,
        int timeLimitMinutes,
        decimal? marksPerCorrect,
        decimal? penaltyPerWrong,
        DateTime? availableFrom,
        DateTime? availableUntil,
        IReadOnlyList<MockExamSectionInput> sections,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Result<Dictionary<Guid, string>>.Failure("Title is required.");
        if (timeLimitMinutes <= 0)
            return Result<Dictionary<Guid, string>>.Failure("Time limit must be greater than zero.");
        if (sections.Count == 0)
            return Result<Dictionary<Guid, string>>.Failure("At least one section with topics is required.");
        if (marksPerCorrect is <= 0)
            return Result<Dictionary<Guid, string>>.Failure("Marks per correct must be greater than zero.");
        if (penaltyPerWrong is < 0)
            return Result<Dictionary<Guid, string>>.Failure("Penalty per wrong cannot be negative.");
        if (availableFrom is not null && availableUntil is not null && availableUntil <= availableFrom)
            return Result<Dictionary<Guid, string>>.Failure("Available until must be after available from.");

        var scope = await _scope.GetSubjectScopeAsync(subjectId, ct);
        if (scope is null)
            return Result<Dictionary<Guid, string>>.Failure("Subject not found.");

        var subjectTopicIds = (await _scope.GetTopicIdsForSubjectAsync(subjectId, ct)).ToHashSet();
        var titles = new Dictionary<Guid, string>();

        foreach (var section in sections)
        {
            if (string.IsNullOrWhiteSpace(section.Title))
                return Result<Dictionary<Guid, string>>.Failure("Each section needs a title.");
            if (section.Topics is null || section.Topics.Count == 0)
                return Result<Dictionary<Guid, string>>.Failure($"Section \"{section.Title}\" needs at least one topic.");

            foreach (var topic in section.Topics)
            {
                if (!subjectTopicIds.Contains(topic.TopicId))
                    return Result<Dictionary<Guid, string>>.Failure("All topics must belong to the selected subject.");
                if (topic.QuestionCount < 0)
                    return Result<Dictionary<Guid, string>>.Failure("Question count cannot be negative.");

                var quiz = await _db.Quizzes.Include(q => q.Questions)
                    .FirstOrDefaultAsync(q => q.TopicId == topic.TopicId, ct);
                if (quiz is null || quiz.Questions.Count == 0)
                    return Result<Dictionary<Guid, string>>.Failure("Each topic must have quiz questions.");

                var take = topic.QuestionCount == 0 ? quiz.Questions.Count : topic.QuestionCount;
                if (take > quiz.Questions.Count)
                    return Result<Dictionary<Guid, string>>.Failure("Question count exceeds available questions for a topic.");

                var topicScope = await _scope.GetTopicScopeAsync(topic.TopicId, ct);
                titles[topic.TopicId] = topicScope?.TopicTitle ?? "Topic";
            }
        }

        return Result<Dictionary<Guid, string>>.Success(titles);
    }

    private Task<MockExam?> LoadExamAsync(Guid id, CancellationToken ct) =>
        _db.MockExams
            .Include(m => m.Sections).ThenInclude(s => s.Topics)
            .Include(m => m.Topics)
            .FirstOrDefaultAsync(m => m.Id == id, ct);

    private static AdminMockExamDto Map(MockExam m)
    {
        var sections = m.Sections
            .OrderBy(s => s.SortOrder)
            .Select(s => new MockExamSectionDto(
                s.Id,
                s.Title,
                s.SortOrder,
                s.SectionTimeLimitMinutes,
                s.Topics.OrderBy(t => t.Order).Select(t => new MockExamTopicDto(
                    t.TopicId, t.TopicTitle, t.QuestionCount, t.Order)).ToList()))
            .ToList();

        var flatTopics = m.Topics
            .OrderBy(t => t.Order)
            .Select(t => new MockExamTopicDto(t.TopicId, t.TopicTitle, t.QuestionCount, t.Order))
            .ToList();

        return new AdminMockExamDto(
            m.Id,
            m.BundleId,
            m.BundleTitle,
            m.SubjectId,
            m.SubjectTitle,
            m.Title,
            m.Description,
            m.TimeLimitMinutes,
            m.MarksPerCorrect,
            m.PenaltyPerWrong,
            m.AvailableFromUtc,
            m.AvailableUntilUtc,
            m.IsPublished,
            m.IsArchived,
            m.ResultVisibility.ToString(),
            m.ShowExplanations,
            m.ResultsPublishedAtUtc,
            m.NotifyTeachersOnBatchComplete,
            m.BatchCompleteThresholdPercent,
            sections,
            flatTopics);
    }
}
