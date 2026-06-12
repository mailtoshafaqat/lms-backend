namespace Lms.Shared.Courses;

/// <summary>Cross-module read contract for subject catalog queries.</summary>
public interface ISubjectCatalogReader
{
    Task<IReadOnlyList<Guid>> GetEnrolledStudentIdsForDefinitionAsync(
        Guid subjectDefinitionId, CancellationToken ct = default);
}
