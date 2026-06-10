using Lms.Modules.Progress.Application;
using Lms.Shared.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Modules.Progress.Api;

[ApiController]
[Route("api/v1/admin/subjects")]
[Authorize(Policy = "Teacher")]
public sealed class AdminProgressController : ControllerBase
{
    private readonly IProgressService _progress;
    private readonly ICurrentUser _currentUser;

    public AdminProgressController(IProgressService progress, ICurrentUser currentUser)
    {
        _progress = progress;
        _currentUser = currentUser;
    }

    [HttpGet("{subjectId:guid}/progress")]
    public async Task<IActionResult> SubjectProgress(Guid subjectId, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;
        var role = _currentUser.Role ?? Roles.Student;
        var result = await _progress.GetSubjectProgressAsync(userId, role, subjectId, ct);

        if (!result.Succeeded)
        {
            return result.Error switch
            {
                "Forbidden" => StatusCode(403, new { error = "You do not have access to this subject." }),
                "Subject not found." => NotFound(new { error = result.Error }),
                _ => BadRequest(new { error = result.Error })
            };
        }

        return Ok(result.Value);
    }

    [HttpGet("{subjectId:guid}/leaderboard")]
    public async Task<IActionResult> SubjectLeaderboard(
        Guid subjectId, [FromQuery] int take = 10, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;
        var role = _currentUser.Role ?? Roles.Student;
        var result = await _progress.GetSubjectLeaderboardAsync(userId, role, subjectId, take, ct);

        if (!result.Succeeded)
        {
            return result.Error switch
            {
                "Forbidden" => StatusCode(403, new { error = "You do not have access to this subject." }),
                "Subject not found." => NotFound(new { error = result.Error }),
                _ => BadRequest(new { error = result.Error })
            };
        }

        return Ok(result.Value);
    }

    [HttpGet("{subjectId:guid}/students/{studentUserId:guid}")]
    public async Task<IActionResult> StudentDetail(
        Guid subjectId, Guid studentUserId, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;
        var role = _currentUser.Role ?? Roles.Student;
        var result = await _progress.GetStudentDetailAsync(userId, role, subjectId, studentUserId, ct);

        if (!result.Succeeded)
        {
            return result.Error switch
            {
                "Forbidden" => StatusCode(403, new { error = "You do not have access to this subject." }),
                "Subject not found." => NotFound(new { error = result.Error }),
                "Student not enrolled in this subject's course." => NotFound(new { error = result.Error }),
                _ => BadRequest(new { error = result.Error })
            };
        }

        return Ok(result.Value);
    }
}
