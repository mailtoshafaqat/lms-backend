namespace Lms.Modules.Assessments.Application;

public sealed record MockExamTopicDto(
    Guid TopicId,
    string TopicTitle,
    int QuestionCount,
    int Order);

public sealed record AdminMockExamDto(
    Guid Id,
    Guid SubjectId,
    string SubjectTitle,
    string Title,
    string? Description,
    int TimeLimitMinutes,
    DateTime? AvailableFromUtc,
    DateTime? AvailableUntilUtc,
    bool IsPublished,
    string ResultVisibility,
    bool ShowExplanations,
    DateTime? ResultsPublishedAtUtc,
    bool NotifyTeachersOnBatchComplete,
    int BatchCompleteThresholdPercent,
    IReadOnlyList<MockExamTopicDto> Topics);

public sealed record MockExamTopicInput(Guid TopicId, int QuestionCount);

public sealed record CreateMockExamRequest(
    Guid SubjectId,
    string Title,
    string? Description,
    int TimeLimitMinutes,
    DateTime? AvailableFromUtc,
    DateTime? AvailableUntilUtc,
    bool IsPublished,
    string? ResultVisibility = null,
    bool? ShowExplanations = null,
    bool? NotifyTeachersOnBatchComplete = null,
    int? BatchCompleteThresholdPercent = null,
    IReadOnlyList<MockExamTopicInput>? Topics = null);

public sealed record UpdateMockExamRequest(
    string Title,
    string? Description,
    int TimeLimitMinutes,
    DateTime? AvailableFromUtc,
    DateTime? AvailableUntilUtc,
    bool IsPublished,
    string? ResultVisibility = null,
    bool? ShowExplanations = null,
    bool? NotifyTeachersOnBatchComplete = null,
    int? BatchCompleteThresholdPercent = null,
    IReadOnlyList<MockExamTopicInput>? Topics = null);

public sealed record MockExamSummaryDto(
    Guid Id,
    Guid SubjectId,
    string SubjectTitle,
    string Title,
    string? Description,
    int TimeLimitMinutes,
    DateTime? AvailableFromUtc,
    DateTime? AvailableUntilUtc,
    string AvailabilityStatus,
    int TotalQuestions,
    ActiveMockAttemptDto? ActiveAttempt);

public sealed record ActiveMockAttemptDto(
    Guid AttemptId,
    DateTime StartedAtUtc,
    DateTime? ExpiresAtUtc);

public sealed record MockExamQuestionDto(
    Guid Id,
    string Stem,
    IReadOnlyList<string> Options,
    int Order);

public sealed record StartMockAttemptResultDto(
    Guid AttemptId,
    DateTime StartedAtUtc,
    DateTime? ExpiresAtUtc,
    IReadOnlyList<MockExamQuestionDto> Questions);

public sealed record MockExamQuestionResultDto(
    Guid QuestionId,
    string Stem,
    IReadOnlyList<string> Options,
    string CorrectKey,
    string? SelectedKey,
    bool IsCorrect,
    string? Explanation);

public sealed record MockExamAttemptResultDto(
    Guid AttemptId,
    int Score,
    int Total,
    bool ResultsVisible,
    string ResultsStatus,
    string? ResultsMessage,
    DateTime? ResultsAvailableAtUtc,
    bool ShowExplanations,
    IReadOnlyList<MockExamQuestionResultDto> Questions);

public sealed record SubmitMockAttemptRequest(
    IReadOnlyList<SubmitAnswerDto> Answers,
    Guid? AttemptId);

public sealed record SubmitAnswerDto(Guid QuestionId, string SelectedKey);
