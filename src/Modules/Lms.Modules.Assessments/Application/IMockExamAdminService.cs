using Lms.Shared.Common;

namespace Lms.Modules.Assessments.Application;

public interface IMockExamAdminService
{
    Task<IReadOnlyList<AdminMockExamDto>> ListForSubjectAsync(Guid subjectId, CancellationToken ct = default);
    Task<AdminMockExamDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<Result<AdminMockExamDto>> CreateAsync(CreateMockExamRequest request, CancellationToken ct = default);
    Task<Result<AdminMockExamDto>> UpdateAsync(Guid id, UpdateMockExamRequest request, CancellationToken ct = default);
    Task<Result<AdminMockExamDto>> PublishResultsAsync(Guid id, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
