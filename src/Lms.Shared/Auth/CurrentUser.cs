using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Lms.Shared.Auth;

public sealed class CurrentUser : ICurrentUser
{
    private readonly ClaimsPrincipal? _user;

    public CurrentUser(IHttpContextAccessor accessor) => _user = accessor.HttpContext?.User;

    public Guid? UserId => _user.GetUserId();

    public string? Email =>
        _user?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
        ?? _user?.FindFirst("email")?.Value;

    public string? Role => _user.GetRole();

    public bool IsAuthenticated => _user?.Identity?.IsAuthenticated ?? false;
}
