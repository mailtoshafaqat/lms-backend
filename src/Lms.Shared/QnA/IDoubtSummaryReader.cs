namespace Lms.Shared.QnA;

public sealed record StudentDoubtSummaryDto(
    int OpenCount,
    int ResolvedCount,
    DateTime? LastActivityAt);

/// <summary>Cross-module read access to doubt stats for student drill-down.</summary>
public interface IDoubtSummaryReader
{
    Task<StudentDoubtSummaryDto> GetSummaryForStudentSubjectAsync(
        Guid studentUserId, Guid subjectId, CancellationToken ct = default);
}
