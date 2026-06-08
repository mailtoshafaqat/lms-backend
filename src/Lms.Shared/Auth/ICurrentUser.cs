namespace Lms.Shared.Auth;

/// <summary>Accessor for the authenticated user of the current request.</summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    string? Email { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
}
