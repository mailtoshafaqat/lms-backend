using Lms.Shared.Common;

namespace Lms.Modules.Assessments.Application;

public interface IMockExamService
{
    Task<IReadOnlyList<MockExamSummaryDto>> ListForUserAsync(Guid userId, CancellationToken ct = default);
    Task<MockExamSummaryDto?> GetForUserAsync(Guid mockExamId, Guid userId, CancellationToken ct = default);
    Task<Result<StartMockAttemptResultDto>> StartAttemptAsync(Guid mockExamId, Guid userId, CancellationToken ct = default);
    Task<Result<MockExamAttemptResultDto>> SubmitAsync(
        Guid mockExamId, Guid userId, SubmitMockAttemptRequest request, CancellationToken ct = default);
    Task<Result<MockExamAttemptResultDto>> GetAttemptResultAsync(
        Guid mockExamId, Guid userId, CancellationToken ct = default);
}
