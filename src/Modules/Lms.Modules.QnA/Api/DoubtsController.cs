using Lms.Modules.QnA.Application;
using Lms.Shared.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Modules.QnA.Api;

[ApiController]
[Route("api/v1/me/doubts")]
[Authorize]
public sealed class DoubtsController : ControllerBase
{
    private readonly IDoubtService _doubts;
    private readonly ICurrentUser _currentUser;

    public DoubtsController(IDoubtService doubts, ICurrentUser currentUser)
    {
        _doubts = doubts;
        _currentUser = currentUser;
    }

    [HttpGet("subjects")]
    public async Task<IActionResult> Subjects(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();
        return Ok(await _doubts.GetEnrolledSubjectsAsync(userId.Value, ct));
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();
        return Ok(await _doubts.ListStudentThreadsAsync(userId.Value, ct));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDoubtRequest request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();

        var result = await _doubts.CreateThreadAsync(
            userId.Value,
            _currentUser.Role ?? Roles.Student,
            request,
            ct);

        return FromResult(result, created: true);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();
        return FromResult(await _doubts.GetStudentThreadAsync(userId.Value, id, ct));
    }

    [HttpPost("{id:guid}/messages")]
    public async Task<IActionResult> AddMessage(Guid id, [FromBody] AddDoubtMessageRequest request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();
        return FromResult(await _doubts.AddStudentMessageAsync(userId.Value, id, request.Body, ct));
    }

    private IActionResult FromResult(
        Shared.Common.Result<DoubtThreadDetailDto> result,
        bool created = false)
    {
        if (result.Succeeded)
            return created ? StatusCode(201, result.Value) : Ok(result.Value);

        return result.Error switch
        {
            DoubtErrors.NotFound => NotFound(new { error = "Thread not found." }),
            DoubtErrors.Forbidden => StatusCode(403, new { error = "You do not have access to this thread." }),
            _ => BadRequest(new { error = result.Error })
        };
    }
}
