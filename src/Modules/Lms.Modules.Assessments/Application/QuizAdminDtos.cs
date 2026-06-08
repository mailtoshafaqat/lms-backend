namespace Lms.Modules.Assessments.Application;

public sealed record CreateQuestionRequest(
    string Stem,
    IReadOnlyList<string> Options,
    string CorrectKey,
    string? Explanation);

public sealed record AdminQuestionDto(
    Guid Id,
    string Stem,
    IReadOnlyList<string> Options,
    string CorrectKey,
    string? Explanation,
    int Order);

public sealed record AdminQuizDto(
    Guid Id,
    Guid TopicId,
    string Title,
    IReadOnlyList<AdminQuestionDto> Questions);

public sealed record UpdateQuestionRequest(
    string Stem,
    IReadOnlyList<string> Options,
    string CorrectKey,
    string? Explanation);

public sealed record UpdateQuizTitleRequest(string Title);

public sealed record ReorderQuestionsRequest(IReadOnlyList<Guid> QuestionIds);
