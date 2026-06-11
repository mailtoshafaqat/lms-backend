using Lms.Modules.Enrollment.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Modules.Enrollment.Api;

[ApiController]
[Route("api/v1/admin/students")]
[Authorize(Policy = "InstituteAdmin")]
public sealed class AdminEnrollmentsController : ControllerBase
{
    private readonly IEnrollmentService _enrollments;

    public AdminEnrollmentsController(IEnrollmentService enrollments) => _enrollments = enrollments;

    [HttpGet("{userId:guid}/enrollments")]
    public async Task<IActionResult> List(Guid userId, CancellationToken ct) =>
        Ok(await _enrollments.GetEnrollmentsForUserAsync(userId, ct));

    [HttpPut("{userId:guid}/enrollments/{bundleId:guid}")]
    public async Task<IActionResult> Extend(
        Guid userId, Guid bundleId, [FromBody] ExtendEnrollmentRequest req, CancellationToken ct)
    {
        var result = await _enrollments.ExtendEnrollmentAsync(userId, bundleId, req.ExpiresAt, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}
