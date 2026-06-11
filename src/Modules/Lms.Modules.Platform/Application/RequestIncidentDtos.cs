namespace Lms.Modules.Platform.Application;

public sealed record RequestIncidentDto(
    Guid Id,
    string TraceId,
    string Method,
    string Path,
    int StatusCode,
    string? ErrorMessage,
    string? ExceptionType,
    string? ExceptionDetail,
    Guid? TenantId,
    string? TenantSlug,
    Guid? UserId,
    string? UserEmail,
    int DurationMs,
    DateTime CreatedAt);

public sealed record RecordRequestIncident(
    string TraceId,
    string Method,
    string Path,
    int StatusCode,
    string? ErrorMessage,
    string? ExceptionType,
    string? ExceptionDetail,
    Guid? TenantId,
    string? TenantSlug,
    Guid? UserId,
    string? UserEmail,
    int DurationMs);
