using Lms.Shared.Entities;

namespace Lms.Modules.Identity.Domain;

/// <summary>Single-use, time-limited token for the forgot-password flow.</summary>
public sealed class PasswordResetToken : BaseEntity
{
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }

    public bool IsValid => UsedAt is null && ExpiresAt > DateTime.UtcNow;
}
