using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Lms.Shared.Auth;

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? User => _accessor.HttpContext?.User;

    public Guid? UserId => User.GetUserId();

    public string? Email =>
        User?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
        ?? User?.FindFirst("email")?.Value;

    public string? Role => User.GetRole();

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
}
