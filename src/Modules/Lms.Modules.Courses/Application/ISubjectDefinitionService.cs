using Lms.Shared.Common;

namespace Lms.Modules.Courses.Application;

public interface ISubjectDefinitionService
{
    Task<IReadOnlyList<SubjectDefinitionDto>> ListAsync(bool activeOnly = false, CancellationToken ct = default);
    Task<SubjectDefinitionDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<Result<SubjectDefinitionDto>> CreateAsync(CreateSubjectDefinitionRequest req, CancellationToken ct = default);
    Task<Result<SubjectDefinitionDto>> UpdateAsync(Guid id, UpdateSubjectDefinitionRequest req, CancellationToken ct = default);
    Task<Result<SubjectDefinitionDto>> ArchiveAsync(Guid id, CancellationToken ct = default);
    Task<Result<UnitDto>> CreateLibraryUnitAsync(Guid definitionId, CreateLibraryUnitRequest req, CancellationToken ct = default);
    Task<IReadOnlyList<UnitDto>> ListLibraryUnitsAsync(Guid definitionId, CancellationToken ct = default);
    Task<Result> LinkSharedUnitsToSubjectAsync(Guid subjectId, LinkSharedUnitsRequest req, CancellationToken ct = default);
}
