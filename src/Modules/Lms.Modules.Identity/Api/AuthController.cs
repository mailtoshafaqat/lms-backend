using System.Security.Claims;
using Lms.Modules.Identity.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Modules.Identity.Api;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        await _auth.ForgotPasswordAsync(request, ct);
        return Ok(new { sent = true });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        var result = await _auth.ResetPasswordAsync(request, ct);
        return result.Succeeded ? Ok(new { reset = true }) : BadRequest(new { error = result.Error });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _auth.LoginAsync(request, ct);
        return result.Succeeded ? Ok(result.Value) : Unauthorized(new { error = result.Error });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
    {
        var result = await _auth.RefreshAsync(request, ct);
        return result.Succeeded ? Ok(result.Value) : Unauthorized(new { error = result.Error });
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var userId = Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty;
        var result = await _auth.ChangePasswordAsync(userId, request, ct);
        return result.Succeeded ? Ok(new { changed = true }) : BadRequest(new { error = result.Error });
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        var profile = new UserProfile(
            Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty,
            User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty,
            User.Identity?.Name ?? string.Empty,
            User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty);
        return Ok(profile);
    }
}
