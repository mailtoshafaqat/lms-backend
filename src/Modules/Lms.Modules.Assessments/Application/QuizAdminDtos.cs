namespace Lms.Modules.Assessments.Application;

public sealed record CreateQuestionRequest(
    string Stem,
    IReadOnlyList<string> Options,
    string CorrectKey,
    string? Explanation,
    bool IsPyq = false,
    int? PyqYear = null,
    string? PyqExam = null,
    string? Difficulty = null);

public sealed record AdminQuestionDto(
    Guid Id,
    string Stem,
    IReadOnlyList<string> Options,
    string CorrectKey,
    string? Explanation,
    int Order,
    string Difficulty,
    bool IsPyq,
    int? PyqYear,
    string? PyqExam);

public sealed record AdminQuizDto(
    Guid Id,
    Guid? TopicId,
    Guid? UnitId,
    string Type,
    string Title,
    int? TimeLimitMinutes,
    DateTime? AvailableFromUtc,
    DateTime? AvailableUntilUtc,
    string ResultVisibility,
    bool ShowExplanations,
    DateTime? ResultsPublishedAtUtc,
    bool NotifyTeachersOnBatchComplete,
    int BatchCompleteThresholdPercent,
    string? DifficultyFilter,
    int AssembledQuestionCount,
    IReadOnlyList<AdminQuestionDto> Questions);

public sealed record AdminUnitQuizDto(
    Guid Id,
    Guid UnitId,
    string Type,
    string Title,
    int? TimeLimitMinutes,
    DateTime? AvailableFromUtc,
    DateTime? AvailableUntilUtc,
    string ResultVisibility,
    bool ShowExplanations,
    DateTime? ResultsPublishedAtUtc,
    bool NotifyTeachersOnBatchComplete,
    int BatchCompleteThresholdPercent,
    string? DifficultyFilter,
    int AssembledQuestionCount);

public sealed record UpdateQuizSettingsRequest(
    int? TimeLimitMinutes,
    DateTime? AvailableFromUtc,
    DateTime? AvailableUntilUtc,
    string? ResultVisibility = null,
    bool? ShowExplanations = null,
    bool? NotifyTeachersOnBatchComplete = null,
    int? BatchCompleteThresholdPercent = null,
    string? DifficultyFilter = null);

public sealed record UpdateQuestionRequest(
    string Stem,
    IReadOnlyList<string> Options,
    string CorrectKey,
    string? Explanation,
    bool IsPyq = false,
    int? PyqYear = null,
    string? PyqExam = null,
    string? Difficulty = null);

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
