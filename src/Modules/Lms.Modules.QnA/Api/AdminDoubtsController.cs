using Lms.Modules.QnA.Application;
using Lms.Shared.Auth;
using Lms.Shared.Common;
using Lms.Shared.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Modules.QnA.Api;

[ApiController]
[Route("api/v1/admin/doubts")]
[Authorize(Policy = "Teacher")]
[RequireProductModule(ProductModule.Doubts)]
public sealed class AdminDoubtsController : ControllerBase
{
    private readonly IDoubtService _doubts;
    private readonly ICurrentUser _currentUser;

    public AdminDoubtsController(IDoubtService doubts, ICurrentUser currentUser)
    {
        _doubts = doubts;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] PagedListQuery query,
        [FromQuery] string? status,
        CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;
        var role = _currentUser.Role ?? Roles.Student;
        return Ok(await _doubts.ListAdminThreadsAsync(userId, role, status, query, ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;
        var role = _currentUser.Role ?? Roles.Student;
        return FromResult(await _doubts.GetAdminThreadAsync(userId, role, id, ct));
    }

    [HttpPost("{id:guid}/reply")]
    public async Task<IActionResult> Reply(Guid id, [FromBody] AddDoubtMessageRequest request, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;
        var role = _currentUser.Role ?? Roles.Student;
        return FromResult(await _doubts.ReplyAsTeacherAsync(userId, role, id, request.Body, ct));
    }

    [HttpPut("{id:guid}/resolve")]
    public async Task<IActionResult> Resolve(Guid id, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;
        var role = _currentUser.Role ?? Roles.Student;
        return FromResult(await _doubts.ResolveThreadAsync(userId, role, id, ct));
    }

    private IActionResult FromResult(Shared.Common.Result<DoubtThreadDetailDto> result)
    {
        if (result.Succeeded) return Ok(result.Value);

        return result.Error switch
        {
            DoubtErrors.NotFound => NotFound(new { error = "Thread not found." }),
            DoubtErrors.Forbidden => StatusCode(403, new { error = "You do not have access to this thread." }),
            _ => BadRequest(new { error = result.Error })
        };
    }
}
