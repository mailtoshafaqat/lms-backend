namespace Lms.Shared.Progress;

public sealed record StudentGradeDto(
    Guid QuizId,
    Guid TopicId,
    string QuizTitle,
    int Score,
    int Total,
    int Percentage,
    DateTime SubmittedAt);

/// <summary>Cross-module read access to student quiz grades for guardian emails.</summary>
public interface IStudentGradesReader
{
    Task<IReadOnlyList<StudentGradeDto>> GetRecentGradesAsync(
        Guid studentUserId, int take = 50, CancellationToken ct = default);
}
