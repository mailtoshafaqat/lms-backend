using Lms.Modules.LiveClasses.Application;
using Lms.Shared.Auth;
using Lms.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Modules.LiveClasses.Api;

[ApiController]
[Route("api/v1/admin/live-classes")]
[Authorize(Policy = "Teacher")]
public sealed class AdminLiveClassesController : ControllerBase
{
    private readonly ILiveClassService _classes;
    private readonly ICurrentUser _currentUser;

    public AdminLiveClassesController(ILiveClassService classes, ICurrentUser currentUser)
    {
        _classes = classes;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] PagedListQuery query,
        [FromQuery] string? state,
        CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;
        var role = _currentUser.Role ?? Roles.Student;
        return Ok(await _classes.ListAsync(userId, role, query, state, ct));
    }

    [HttpGet("zoom-status")]
    public async Task<IActionResult> ZoomStatus(CancellationToken ct) =>
        Ok(new { configured = await _classes.IsZoomConfiguredAsync(ct) });

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLiveClassRequest req, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;
        var role = _currentUser.Role ?? Roles.Student;
        var result = await _classes.CreateAsync(userId, role, req, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct) =>
        await _classes.CancelAsync(id, ct) ? NoContent() : NotFound();

    [HttpPut("{id:guid}/recording")]
    public async Task<IActionResult> AttachRecording(Guid id, [FromBody] AttachRecordingRequest req, CancellationToken ct)
    {
        var result = await _classes.AttachRecordingAsync(id, req, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpGet("{id:guid}/attendance")]
    public async Task<IActionResult> Attendance(Guid id, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;
        var role = _currentUser.Role ?? Roles.Student;
        var result = await _classes.GetAttendanceAsync(userId, role, id, ct);

        if (!result.Succeeded)
        {
            return result.Error switch
            {
                "Forbidden" => StatusCode(403, new { error = "You do not have access to this class." }),
                "Live class not found." => NotFound(new { error = result.Error }),
                _ => BadRequest(new { error = result.Error })
            };
        }

        return Ok(result.Value);
    }
}
