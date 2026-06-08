namespace Lms.Modules.Assessments.Application;

/// <summary>Question as shown to a student taking the quiz — NO correct answer leaked.</summary>
public sealed record QuizQuestionDto(Guid Id, string Stem, IReadOnlyList<string> Options, int Order);

public sealed record QuizDto(Guid Id, Guid TopicId, string Title, IReadOnlyList<QuizQuestionDto> Questions);

public sealed record SubmitAnswer(Guid QuestionId, string SelectedKey);

public sealed record SubmitAttemptRequest(IReadOnlyList<SubmitAnswer> Answers);

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
    IReadOnlyList<QuestionResultDto> Questions);
