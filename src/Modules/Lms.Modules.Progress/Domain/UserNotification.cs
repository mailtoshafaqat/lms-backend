using Lms.Shared.Entities;

namespace Lms.Modules.Progress.Domain;

/// <summary>In-app notification for a student (optionally paired with email).</summary>
public sealed class UserNotification : TenantEntity
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? LinkUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
}
