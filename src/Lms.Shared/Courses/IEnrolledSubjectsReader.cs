namespace Lms.Shared.Courses;

/// <summary>Cross-module read contract for a student's enrolled subjects.
/// Implemented by the Courses module; used by QnA for the ask-teacher subject picker.</summary>
public interface IEnrolledSubjectsReader
{
    Task<IReadOnlyList<AssignedSubjectDto>> GetEnrolledSubjectsAsync(
        Guid userId, CancellationToken ct = default);
}
