using Lms.Shared.Common;

namespace Lms.Modules.Assessments.Application;

public interface IMockExamAdminService
{
    Task<IReadOnlyList<AdminMockExamDto>> ListForSubjectAsync(
        Guid subjectId, bool includeArchived = false, CancellationToken ct = default);

    Task<IReadOnlyList<AdminMockExamDto>> ListForBundleAsync(
        Guid bundleId, bool includeArchived = false, CancellationToken ct = default);

    Task<AdminMockExamDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<Result<AdminMockExamDto>> CreateAsync(CreateMockExamRequest request, CancellationToken ct = default);
    Task<Result<AdminMockExamDto>> UpdateAsync(Guid id, UpdateMockExamRequest request, CancellationToken ct = default);
    Task<Result<AdminMockExamDto>> PublishResultsAsync(Guid id, CancellationToken ct = default);
    Task<Result<AdminMockExamDto>> SetArchivedAsync(Guid id, bool isArchived, CancellationToken ct = default);
    Task<Result<MockExamLeaderboardDto>> GetLeaderboardAsync(
        Guid mockExamId, Guid? currentUserId, int take = 100, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
