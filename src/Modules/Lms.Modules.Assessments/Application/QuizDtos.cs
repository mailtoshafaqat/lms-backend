namespace Lms.Modules.Assessments.Application;

/// <summary>Question as shown to a student taking the quiz — NO correct answer leaked.</summary>
public sealed record QuizQuestionDto(Guid Id, string Stem, IReadOnlyList<string> Options, int Order);

public sealed record ActiveAttemptDto(
    Guid AttemptId,
    DateTime StartedAtUtc,
    DateTime? ExpiresAtUtc);

/// <summary>Student-facing quiz payload with optional scheduling metadata.</summary>
public sealed record QuizDto(
    Guid Id,
    Guid? TopicId,
    Guid? UnitId,
    string Type,
    string Title,
    int? TimeLimitMinutes,
    DateTime? AvailableFromUtc,
    DateTime? AvailableUntilUtc,
    string AvailabilityStatus,
    string ResultVisibility,
    bool ShowExplanations,
    string? DifficultyFilter,
    IReadOnlyList<string> AvailableDifficulties,
    ActiveAttemptDto? ActiveAttempt,
    IReadOnlyList<QuizQuestionDto> Questions,
    IReadOnlyList<Guid> FlaggedQuestionIds);

public sealed record SubmitAnswer(Guid QuestionId, string SelectedKey);

public sealed record SubmitAttemptRequest(
    IReadOnlyList<SubmitAnswer> Answers,
    Guid? AttemptId,
    IReadOnlyList<Guid>? FlaggedQuestionIds = null);

public sealed record StartAttemptResultDto(
    Guid AttemptId,
    DateTime StartedAtUtc,
    DateTime? ExpiresAtUtc,
    IReadOnlyList<QuizQuestionDto> Questions,
    IReadOnlyList<Guid> FlaggedQuestionIds);

public sealed record QuestionResultDto(
    Guid QuestionId,
    string Stem,
    IReadOnlyList<string> Options,
    string CorrectKey,
    string? SelectedKey,
    bool IsCorrect,
    string? Explanation);

public sealed record AttemptResultDto(
    Guid AttemptId,
    int Score,
    int Total,
    bool ResultsVisible,
    string ResultsStatus,
    string? ResultsMessage,
    DateTime? ResultsAvailableAtUtc,
    bool ShowExplanations,
    IReadOnlyList<QuestionResultDto> Questions);
