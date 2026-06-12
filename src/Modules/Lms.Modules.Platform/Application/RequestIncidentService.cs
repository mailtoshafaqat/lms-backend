using Lms.Modules.Platform.Domain;
using Lms.Modules.Platform.Infrastructure;
using Lms.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Platform.Application;

public sealed class RequestIncidentService : IRequestIncidentService
{
    private readonly PlatformDbContext _db;

    public RequestIncidentService(PlatformDbContext db) => _db = db;

    public async Task RecordAsync(RecordRequestIncident incident, CancellationToken ct = default)
    {
        _db.RequestIncidents.Add(new RequestIncident
        {
            Id = Guid.NewGuid(),
            TraceId = incident.TraceId,
            Method = incident.Method,
            Path = Truncate(incident.Path, 512),
            StatusCode = incident.StatusCode,
            ErrorMessage = Truncate(incident.ErrorMessage, 2000),
            ExceptionType = Truncate(incident.ExceptionType, 256),
            ExceptionDetail = Truncate(incident.ExceptionDetail, 4000),
            TenantId = incident.TenantId,
            TenantSlug = Truncate(incident.TenantSlug, 64),
            UserId = incident.UserId,
            UserEmail = Truncate(incident.UserEmail, 256),
            DurationMs = incident.DurationMs,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<RequestIncidentDto>> SearchAsync(
        string? traceId, PagedListQuery paging, CancellationToken ct = default)
    {
        IQueryable<RequestIncident> query = _db.RequestIncidents.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(traceId))
        {
            var normalized = traceId.Trim();
            query = query.Where(i => i.TraceId.Contains(normalized));
        }

        query = query.OrderByDescending(i => i.CreatedAt);

        return await query
            .Select(i => new RequestIncidentDto(
                i.Id, i.TraceId, i.Method, i.Path, i.StatusCode, i.ErrorMessage, i.ExceptionType,
                i.ExceptionDetail, i.TenantId, i.TenantSlug, i.UserId, i.UserEmail, i.DurationMs, i.CreatedAt))
            .ToPagedResultAsync(paging, ct);
    }

    private static string? Truncate(string? value, int max) =>
        value is null ? null : value.Length <= max ? value : value[..max];
}
