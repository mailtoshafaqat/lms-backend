using Lms.Shared.Auth;
using Lms.Shared.Entities;

namespace Lms.Modules.Identity.Domain;

public enum AuthProvider
{
    Local = 0,
    Google = 1
}

/// <summary>An account within a tenant. Email is unique per tenant.</summary>
public sealed class User : TenantEntity
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    /// <summary>Student contact number (set by institute admin).</summary>
    public string? Phone { get; set; }

    /// <summary>Public URL path e.g. /api/v1/files/students/…</summary>
    public string? ProfilePictureUrl { get; set; }

    /// <summary>Internal admin notes about the student (not shown to the student).</summary>
    public string? ProfileNotes { get; set; }

    public string? PasswordHash { get; set; }
    public string Role { get; set; } = Roles.Student;
    public AuthProvider Provider { get; set; } = AuthProvider.Local;
    public bool IsActive { get; set; } = true;

    /// <summary>True for admin-provisioned accounts using a temporary password; forces a reset on first login.</summary>
    public bool MustChangePassword { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
