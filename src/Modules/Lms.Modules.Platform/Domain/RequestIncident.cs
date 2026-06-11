namespace Lms.Modules.Platform.Domain;

/// <summary>Stored API error for support lookup by trace id (cross-tenant).</summary>
public sealed class RequestIncident
{
    public Guid Id { get; set; }
    public string TraceId { get; set; } = "";
    public string Method { get; set; } = "";
    public string Path { get; set; } = "";
    public int StatusCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ExceptionType { get; set; }
    /// <summary>Stack trace / technical detail — Support + SuperAdmin only, never returned to end users.</summary>
    public string? ExceptionDetail { get; set; }
    public Guid? TenantId { get; set; }
    public string? TenantSlug { get; set; }
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public int DurationMs { get; set; }
    public DateTime CreatedAt { get; set; }
}
