using Lms.Shared.Common;

namespace Lms.Modules.Platform.Application;

public interface IRequestIncidentService
{
    Task RecordAsync(RecordRequestIncident incident, CancellationToken ct = default);
    Task<PagedResult<RequestIncidentDto>> SearchAsync(
        string? traceId, PagedListQuery paging, CancellationToken ct = default);
}
