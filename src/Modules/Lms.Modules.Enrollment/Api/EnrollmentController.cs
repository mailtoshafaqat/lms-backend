using Lms.Modules.Enrollment.Application;
using Lms.Shared.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Modules.Enrollment.Api;

[ApiController]
[Route("api/v1")]
[Authorize]
public sealed class EnrollmentController : ControllerBase
{
    private readonly IEnrollmentService _enrollments;
    private readonly ICurrentUser _currentUser;

    public EnrollmentController(IEnrollmentService enrollments, ICurrentUser currentUser)
    {
        _enrollments = enrollments;
        _currentUser = currentUser;
    }

    [HttpPost("bundles/{bundleId:guid}/enroll")]
    public async Task<IActionResult> Enroll(Guid bundleId, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();

        var result = await _enrollments.EnrollAsync(userId.Value, bundleId, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpGet("me/enrollments")]
    public async Task<IActionResult> MyEnrollments(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();
        return Ok(await _enrollments.GetMyEnrollmentsAsync(userId.Value, ct));
    }
}
