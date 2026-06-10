using Lms.Modules.Assessments.Domain;
using Lms.Modules.Assessments.Infrastructure;
using Lms.Shared.Common;
using Lms.Shared.Courses;
using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Assessments.Application;

public sealed class MockExamAdminService : IMockExamAdminService
{
    private readonly AssessmentsDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICourseScopeReader _scope;

    public MockExamAdminService(AssessmentsDbContext db, ITenantContext tenant, ICourseScopeReader scope)
    {
        _db = db;
        _tenant = tenant;
        _scope = scope;
    }

    public async Task<IReadOnlyList<AdminMockExamDto>> ListForSubjectAsync(
        Guid subjectId, CancellationToken ct = default)
    {
        var rows = await _db.MockExams
            .Include(m => m.Topics)
            .Where(m => m.SubjectId == subjectId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(ct);

        return rows.Select(Map).ToList();
    }

    public async Task<AdminMockExamDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var row = await _db.MockExams.Include(m => m.Topics).FirstOrDefaultAsync(m => m.Id == id, ct);
        return row is null ? null : Map(row);
    }

    public async Task<Result<AdminMockExamDto>> CreateAsync(
        CreateMockExamRequest request, CancellationToken ct = default)
    {
        var validation = await ValidateRequestAsync(request.SubjectId, request.Title, request.TimeLimitMinutes,
            request.AvailableFromUtc, request.AvailableUntilUtc, request.Topics, ct);
        if (!validation.Succeeded) return Result<AdminMockExamDto>.Failure(validation.Error!);

        var scope = await _scope.GetSubjectScopeAsync(request.SubjectId, ct)!;
        var topicTitles = validation.Value!;

        var entity = new MockExam
        {
            TenantId = _tenant.TenantId,
            SubjectId = request.SubjectId,
            SubjectTitle = scope!.SubjectTitle,
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            TimeLimitMinutes = request.TimeLimitMinutes,
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

        var order = 1;
        foreach (var topic in request.Topics)
        {
            entity.Topics.Add(new MockExamTopic
            {
                TenantId = _tenant.TenantId,
                TopicId = topic.TopicId,
                TopicTitle = topicTitles[topic.TopicId],
                QuestionCount = topic.QuestionCount,
                Order = order++
            });
        }

        _db.MockExams.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Result<AdminMockExamDto>.Success(Map(entity));
    }

    public async Task<Result<AdminMockExamDto>> UpdateAsync(
        Guid id, UpdateMockExamRequest request, CancellationToken ct = default)
    {
        var entity = await _db.MockExams.Include(m => m.Topics).FirstOrDefaultAsync(m => m.Id == id, ct);
        if (entity is null) return Result<AdminMockExamDto>.Failure("Mock exam not found.");

        var validation = await ValidateRequestAsync(entity.SubjectId, request.Title, request.TimeLimitMinutes,
            request.AvailableFromUtc, request.AvailableUntilUtc, request.Topics, ct);
        if (!validation.Succeeded) return Result<AdminMockExamDto>.Failure(validation.Error!);

        var topicTitles = validation.Value!;
        ApplyPolicyFields(entity, request.ResultVisibility, request.ShowExplanations,
            request.NotifyTeachersOnBatchComplete, request.BatchCompleteThresholdPercent,
            request.AvailableUntilUtc);
        entity.Title = request.Title.Trim();
        entity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        entity.TimeLimitMinutes = request.TimeLimitMinutes;
        entity.AvailableFromUtc = request.AvailableFromUtc;
        entity.AvailableUntilUtc = request.AvailableUntilUtc;
        entity.IsPublished = request.IsPublished;

        if (entity.ResultVisibility == ResultVisibilityMode.AfterClose && entity.AvailableUntilUtc is null)
            return Result<AdminMockExamDto>.Failure("After-close visibility requires an end date.");

        _db.MockExamTopics.RemoveRange(entity.Topics);
        entity.Topics.Clear();

        var order = 1;
        foreach (var topic in request.Topics)
        {
            entity.Topics.Add(new MockExamTopic
            {
                TenantId = _tenant.TenantId,
                TopicId = topic.TopicId,
                TopicTitle = topicTitles[topic.TopicId],
                QuestionCount = topic.QuestionCount,
                Order = order++
            });
        }

        await _db.SaveChangesAsync(ct);
        return Result<AdminMockExamDto>.Success(Map(entity));
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.MockExams.Include(m => m.Topics).FirstOrDefaultAsync(m => m.Id == id, ct);
        if (entity is null) return false;

        _db.MockExamTopics.RemoveRange(entity.Topics);
        _db.MockExams.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<Result<AdminMockExamDto>> PublishResultsAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.MockExams.Include(m => m.Topics).FirstOrDefaultAsync(m => m.Id == id, ct);
        if (entity is null) return Result<AdminMockExamDto>.Failure("Mock exam not found.");

        if (entity.ResultVisibility != ResultVisibilityMode.ManualPublish)
            return Result<AdminMockExamDto>.Failure("Results can only be published when visibility is ManualPublish.");

        entity.ResultsPublishedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Result<AdminMockExamDto>.Success(Map(entity));
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
        DateTime? availableFrom,
        DateTime? availableUntil,
        IReadOnlyList<MockExamTopicInput> topics,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Result<Dictionary<Guid, string>>.Failure("Title is required.");
        if (timeLimitMinutes <= 0)
            return Result<Dictionary<Guid, string>>.Failure("Time limit must be greater than zero.");
        if (topics is null || topics.Count == 0)
            return Result<Dictionary<Guid, string>>.Failure("At least one topic is required.");
        if (availableFrom is not null && availableUntil is not null && availableUntil <= availableFrom)
            return Result<Dictionary<Guid, string>>.Failure("Available until must be after available from.");

        var scope = await _scope.GetSubjectScopeAsync(subjectId, ct);
        if (scope is null)
            return Result<Dictionary<Guid, string>>.Failure("Subject not found.");

        var subjectTopicIds = (await _scope.GetTopicIdsForSubjectAsync(subjectId, ct)).ToHashSet();
        var titles = new Dictionary<Guid, string>();

        foreach (var topic in topics)
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

        return Result<Dictionary<Guid, string>>.Success(titles);
    }

    private static AdminMockExamDto Map(MockExam m) => new(
        m.Id,
        m.SubjectId,
        m.SubjectTitle,
        m.Title,
        m.Description,
        m.TimeLimitMinutes,
        m.AvailableFromUtc,
        m.AvailableUntilUtc,
        m.IsPublished,
        m.ResultVisibility.ToString(),
        m.ShowExplanations,
        m.ResultsPublishedAtUtc,
        m.NotifyTeachersOnBatchComplete,
        m.BatchCompleteThresholdPercent,
        m.Topics.OrderBy(t => t.Order).Select(t => new MockExamTopicDto(
            t.TopicId, t.TopicTitle, t.QuestionCount, t.Order)).ToList());
}
