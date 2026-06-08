using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Lms.Shared.Auth;

public sealed class CurrentUser : ICurrentUser
{
    private readonly ClaimsPrincipal? _user;

    public CurrentUser(IHttpContextAccessor accessor) => _user = accessor.HttpContext?.User;

    public Guid? UserId =>
        Guid.TryParse(_user?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

    public string? Email => _user?.FindFirst(ClaimTypes.Email)?.Value;

    public string? Role => _user?.FindFirst(ClaimTypes.Role)?.Value;

    public bool IsAuthenticated => _user?.Identity?.IsAuthenticated ?? false;
}
