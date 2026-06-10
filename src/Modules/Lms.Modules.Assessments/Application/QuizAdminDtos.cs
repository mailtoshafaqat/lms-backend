namespace Lms.Modules.Assessments.Application;

public sealed record CreateQuestionRequest(
    string Stem,
    IReadOnlyList<string> Options,
    string CorrectKey,
    string? Explanation,
    bool IsPyq = false,
    int? PyqYear = null,
    string? PyqExam = null);

public sealed record AdminQuestionDto(
    Guid Id,
    string Stem,
    IReadOnlyList<string> Options,
    string CorrectKey,
    string? Explanation,
    int Order,
    bool IsPyq,
    int? PyqYear,
    string? PyqExam);

public sealed record AdminQuizDto(
    Guid Id,
    Guid TopicId,
    string Title,
    int? TimeLimitMinutes,
    DateTime? AvailableFromUtc,
    DateTime? AvailableUntilUtc,
    string ResultVisibility,
    bool ShowExplanations,
    DateTime? ResultsPublishedAtUtc,
    bool NotifyTeachersOnBatchComplete,
    int BatchCompleteThresholdPercent,
    IReadOnlyList<AdminQuestionDto> Questions);

public sealed record UpdateQuizSettingsRequest(
    int? TimeLimitMinutes,
    DateTime? AvailableFromUtc,
    DateTime? AvailableUntilUtc,
    string? ResultVisibility = null,
    bool? ShowExplanations = null,
    bool? NotifyTeachersOnBatchComplete = null,
    int? BatchCompleteThresholdPercent = null);

public sealed record UpdateQuestionRequest(
    string Stem,
    IReadOnlyList<string> Options,
    string CorrectKey,
    string? Explanation,
    bool IsPyq = false,
    int? PyqYear = null,
    string? PyqExam = null);

public sealed record QuestionAnalyticsDto(
    Guid QuestionId,
    string Stem,
    int AttemptCount,
    int WrongCount,
    int WrongPercentage);

public sealed record QuizAnalyticsDto(
    Guid QuizId,
    Guid TopicId,
    string Title,
    int TotalAttempts,
    IReadOnlyList<QuestionAnalyticsDto> Questions);

public sealed record UpdateQuizTitleRequest(string Title);

public sealed record ReorderQuestionsRequest(IReadOnlyList<Guid> QuestionIds);
