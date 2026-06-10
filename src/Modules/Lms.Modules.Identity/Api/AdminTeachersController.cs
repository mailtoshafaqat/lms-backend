using Lms.Modules.Identity.Application;
using Lms.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Modules.Identity.Api;

[ApiController]
[Route("api/v1/admin/teachers")]
[Authorize(Policy = "InstituteAdmin")]
public sealed class AdminTeachersController : ControllerBase
{
    private readonly IAdminUserService _users;

    public AdminTeachersController(IAdminUserService users) => _users = users;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PagedListQuery query, CancellationToken ct) =>
        Ok(await _users.ListTeachersAsync(query, ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTeacherRequest req, CancellationToken ct)
    {
        var result = await _users.CreateTeacherAsync(req, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPut("{userId:guid}/status")]
    public async Task<IActionResult> SetStatus(
        Guid userId, [FromBody] SetTeacherStatusRequest req, CancellationToken ct)
    {
        var result = await _users.SetTeacherStatusAsync(userId, req.IsActive, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{userId:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid userId, CancellationToken ct)
    {
        var result = await _users.ResetTeacherPasswordAsync(userId, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}
