namespace Lms.Shared.Assessments;

public sealed record QuestionSnapshot(
    Guid Id,
    Guid TopicId,
    Guid QuizId,
    string Stem,
    IReadOnlyList<string> Options,
    string CorrectKey,
    string? Explanation);

/// <summary>Read-only access to MCQ details for cross-module features (e.g. mistake diary).</summary>
public interface IQuestionReader
{
    Task<IReadOnlyList<QuestionSnapshot>> GetByIdsAsync(IEnumerable<Guid> questionIds, CancellationToken ct = default);
}
