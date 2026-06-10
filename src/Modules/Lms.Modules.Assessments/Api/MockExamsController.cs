using Lms.Modules.Assessments.Application;
using Lms.Shared.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Modules.Assessments.Api;

[ApiController]
[Route("api/v1")]
[Authorize]
public sealed class MockExamsController : ControllerBase
{
    private readonly IMockExamService _mockExams;
    private readonly ICurrentUser _currentUser;

    public MockExamsController(IMockExamService mockExams, ICurrentUser currentUser)
    {
        _mockExams = mockExams;
        _currentUser = currentUser;
    }

    [HttpGet("me/mock-exams")]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();
        return Ok(await _mockExams.ListForUserAsync(userId.Value, ct));
    }

    [HttpGet("mock-exams/{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();
        var exam = await _mockExams.GetForUserAsync(id, userId.Value, ct);
        return exam is null ? NotFound() : Ok(exam);
    }

    [HttpPost("mock-exams/{id:guid}/attempts/start")]
    public async Task<IActionResult> StartAttempt(Guid id, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();
        var result = await _mockExams.StartAttemptAsync(id, userId.Value, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("mock-exams/{id:guid}/attempts")]
    public async Task<IActionResult> Submit(Guid id, [FromBody] SubmitMockAttemptRequest request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();
        var result = await _mockExams.SubmitAsync(id, userId.Value, request, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpGet("mock-exams/{id:guid}/attempts/result")]
    public async Task<IActionResult> GetAttemptResult(Guid id, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();
        var result = await _mockExams.GetAttemptResultAsync(id, userId.Value, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}
