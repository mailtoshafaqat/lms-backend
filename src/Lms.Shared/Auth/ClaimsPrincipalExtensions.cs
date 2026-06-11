using System.Security.Claims;

namespace Lms.Shared.Auth;

public static class ClaimsPrincipalExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal? user)
    {
        var raw = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? user?.FindFirst("sub")?.Value;
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    public static string? GetRole(this ClaimsPrincipal? user)
    {
        var role = user?.FindFirst(ClaimTypes.Role)?.Value
                   ?? user?.FindFirst("role")?.Value;
        return string.IsNullOrWhiteSpace(role) ? null : role;
    }

    public static bool HasInstituteWideAccess(this ClaimsPrincipal? user)
    {
        if (user is null) return false;
        var role = user.GetRole();
        if (role is Roles.SuperAdmin or Roles.InstituteAdmin) return true;
        return user.IsInRole(Roles.SuperAdmin) || user.IsInRole(Roles.InstituteAdmin);
    }
}
