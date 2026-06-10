using Lms.Modules.Courses.Application;
using Lms.Shared.Auth;
using Lms.Shared.Courses;
using Lms.Shared.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Modules.Courses.Api;

[ApiController]
[Route("api/v1/admin")]
public sealed class AdminSubjectTeachersController : ControllerBase
{
    private readonly ISubjectAccessService _access;
    private readonly ICurrentUser _currentUser;
    private readonly IUserDirectory _users;

    public AdminSubjectTeachersController(
        ISubjectAccessService access, ICurrentUser currentUser, IUserDirectory users)
    {
        _access = access;
        _currentUser = currentUser;
        _users = users;
    }

    [HttpGet("my-subjects")]
    [Authorize(Policy = "Teacher")]
    public async Task<IActionResult> MySubjects(CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;
        var role = _currentUser.Role ?? Roles.Student;
        return Ok(await _access.GetAssignedSubjectsAsync(userId, role, ct));
    }

    [HttpGet("me/profile")]
    [Authorize(Policy = "Teacher")]
    public async Task<IActionResult> MyProfile(CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;
        var role = _currentUser.Role ?? Roles.Student;
        var subjects = await _access.GetAssignedSubjectsAsync(userId, role, ct);
        var names = await _users.GetDisplayNamesAsync([userId], ct);

        return Ok(new AdminProfileDto(
            userId,
            _currentUser.Email ?? string.Empty,
            names.TryGetValue(userId, out var name) ? name : string.Empty,
            role,
            subjects));
    }

    [HttpGet("subject-teachers")]
    [Authorize(Policy = "InstituteAdmin")]
    public async Task<IActionResult> ListAssignments(CancellationToken ct) =>
        Ok(await _access.ListAssignmentsAsync(ct));

    [HttpPut("teachers/{userId:guid}/subjects")]
    [Authorize(Policy = "InstituteAdmin")]
    public async Task<IActionResult> SetSubjects(
        Guid userId, [FromBody] SetTeacherSubjectsRequest req, CancellationToken ct)
    {
        var result = await _access.SetTeacherSubjectsAsync(userId, req.SubjectIds, ct);
        return result.Succeeded ? Ok(new { saved = true }) : BadRequest(new { error = result.Error });
    }
}

public sealed record SetTeacherSubjectsRequest(IReadOnlyList<Guid> SubjectIds);

public sealed record AdminProfileDto(
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    IReadOnlyList<AssignedSubjectDto> Subjects);
