namespace Lms.Modules.Progress.Application;

public sealed record WeaknessQuizQuestionDto(Guid Id, string Stem, IReadOnlyList<string> Options, int Order);

public sealed record WeaknessQuizDto(
    Guid SessionId,
    string Title,
    string Source,
    IReadOnlyList<WeaknessQuizQuestionDto> Questions);

public sealed record WeaknessQuizAnswerDto(Guid QuestionId, string SelectedKey);

public sealed record SubmitWeaknessQuizRequest(IReadOnlyList<WeaknessQuizAnswerDto> Answers);

public sealed record WeaknessQuestionResultDto(
    Guid QuestionId,
    string Stem,
    IReadOnlyList<string> Options,
    string CorrectKey,
    string? SelectedKey,
    bool IsCorrect,
    string? Explanation);

public sealed record WeaknessQuizResultDto(
    int Score,
    int Total,
    int ResolvedMistakes,
    IReadOnlyList<WeaknessQuestionResultDto> Questions);
