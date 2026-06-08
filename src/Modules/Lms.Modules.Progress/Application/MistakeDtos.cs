namespace Lms.Modules.Progress.Application;

public sealed record MistakeDto(
    Guid Id,
    Guid QuestionId,
    Guid TopicId,
    Guid QuizId,
    string QuizTitle,
    string Stem,
    IReadOnlyList<string> Options,
    string CorrectKey,
    string? LastSelectedKey,
    string? Explanation,
    int TimesWrong,
    DateTime LastSeenAt);
