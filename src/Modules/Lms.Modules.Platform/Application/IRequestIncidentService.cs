namespace Lms.Modules.Platform.Application;

public interface IRequestIncidentService
{
    Task RecordAsync(RecordRequestIncident incident, CancellationToken ct = default);
    Task<IReadOnlyList<RequestIncidentDto>> SearchAsync(string? traceId, int take = 25, CancellationToken ct = default);
}
