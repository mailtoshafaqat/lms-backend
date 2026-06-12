using Lms.Modules.Identity.Application;
using Lms.Shared.Common;
using Lms.Shared.Enrollments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Modules.Identity.Api;

[ApiController]
[Route("api/v1/admin/students")]
[Authorize(Policy = "InstituteAdmin")]
public sealed class AdminUsersController : ControllerBase
{
    private readonly IAdminUserService _users;
    private readonly IEnrollmentWriter _enrollments;

    public AdminUsersController(IAdminUserService users, IEnrollmentWriter enrollments)
    {
        _users = users;
        _enrollments = enrollments;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] PagedListQuery query,
        [FromQuery] Guid? subjectDefinitionId,
        CancellationToken ct)
    {
        if (subjectDefinitionId is Guid id)
            query.SubjectDefinitionId = id;
        return Ok(await _users.ListStudentsAsync(query, ct));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStudentRequest req, CancellationToken ct)
    {
        var result = await _users.CreateStudentAsync(req, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{userId:guid}/enroll")]
    public async Task<IActionResult> Enroll(Guid userId, [FromBody] EnrollStudentRequest req, CancellationToken ct)
    {
        var summary = await _enrollments.EnrollAsync(userId, req.BundleId, ct);
        return summary is null
            ? BadRequest(new { error = "Enrollment failed. Check bundle id or existing enrollment." })
            : Ok(summary);
    }

    [HttpPut("{userId:guid}/status")]
    public async Task<IActionResult> SetStatus(
        Guid userId, [FromBody] SetStudentStatusRequest req, CancellationToken ct)
    {
        var result = await _users.SetStudentStatusAsync(userId, req.IsActive, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{userId:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid userId, CancellationToken ct)
    {
        var result = await _users.ResetStudentPasswordAsync(userId, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpGet("{userId:guid}/profile")]
    public async Task<IActionResult> GetProfile(Guid userId, CancellationToken ct)
    {
        var profile = await _users.GetStudentProfileAsync(userId, ct);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPut("{userId:guid}/profile")]
    public async Task<IActionResult> UpdateProfile(
        Guid userId, [FromBody] UpdateStudentProfileRequest req, CancellationToken ct)
    {
        var result = await _users.UpdateStudentProfileAsync(userId, req, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpGet("{userId:guid}/guardians")]
    public async Task<IActionResult> ListGuardians(Guid userId, CancellationToken ct) =>
        Ok(await _users.ListGuardiansAsync(userId, ct));

    [HttpPost("{userId:guid}/guardians")]
    public async Task<IActionResult> CreateGuardian(
        Guid userId, [FromBody] CreateStudentGuardianRequest req, CancellationToken ct)
    {
        var result = await _users.CreateGuardianAsync(userId, req, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPut("{userId:guid}/guardians/{guardianId:guid}")]
    public async Task<IActionResult> UpdateGuardian(
        Guid userId, Guid guardianId, [FromBody] UpdateStudentGuardianRequest req, CancellationToken ct)
    {
        var result = await _users.UpdateGuardianAsync(guardianId, req, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpDelete("{userId:guid}/guardians/{guardianId:guid}")]
    public async Task<IActionResult> DeleteGuardian(Guid userId, Guid guardianId, CancellationToken ct) =>
        await _users.DeleteGuardianAsync(guardianId, ct) ? NoContent() : NotFound();

    [HttpPost("{userId:guid}/guardians/{guardianId:guid}/send-report")]
    public async Task<IActionResult> SendGuardianReport(Guid userId, Guid guardianId, CancellationToken ct)
    {
        var result = await _users.SendGuardianReportAsync(userId, guardianId, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}
