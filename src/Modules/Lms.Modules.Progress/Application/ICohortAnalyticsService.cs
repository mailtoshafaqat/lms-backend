using Lms.Shared.Common;

namespace Lms.Modules.Progress.Application;

public interface ICohortAnalyticsService
{
    Task<Result<CohortAnalyticsOverviewDto>> GetOverviewAsync(
        Guid bundleId, Guid? subjectId, DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default);

    Task<Result<IReadOnlyList<CohortStudentRowDto>>> GetStudentRowsAsync(
        Guid bundleId, Guid? subjectId, DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default);

    Task<Result<byte[]>> ExportCsvAsync(
        Guid bundleId, Guid? subjectId, DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default);
}
