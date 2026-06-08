using Lms.Shared.Entities;

namespace Lms.Modules.Identity.Domain;

public sealed class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    public bool IsActive => RevokedAt is null && DateTime.UtcNow < ExpiresAt;
}
