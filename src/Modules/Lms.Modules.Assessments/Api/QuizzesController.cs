using Lms.Modules.Assessments.Application;
using Lms.Shared.Auth;
using Lms.Shared.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Modules.Assessments.Api;

[ApiController]
[Route("api/v1")]
public sealed class QuizzesController : ControllerBase
{
    private readonly IQuizService _quizzes;
    private readonly ICurrentUser _currentUser;

    public QuizzesController(IQuizService quizzes, ICurrentUser currentUser)
    {
        _quizzes = quizzes;
        _currentUser = currentUser;
    }

    [HttpGet("topics/{topicId:guid}/quiz")]
    public async Task<IActionResult> GetByTopic(
        Guid topicId, [FromQuery] string? difficulty, CancellationToken ct)
    {
        var quiz = await _quizzes.GetByTopicAsync(topicId, _currentUser.UserId, difficulty, ct);
        return quiz is null ? NotFound() : Ok(quiz);
    }

    [HttpGet("units/{unitId:guid}/quizzes/{quizType}")]
    [RequireProductModule(ProductModule.UnitPyqTests)]
    public async Task<IActionResult> GetByUnit(
        Guid unitId, string quizType, [FromQuery] string? difficulty, CancellationToken ct)
    {
        var quiz = await _quizzes.GetByUnitAsync(unitId, quizType, _currentUser.UserId, difficulty, ct);
        return quiz is null ? NotFound() : Ok(quiz);
    }

    [HttpGet("quizzes/{quizId:guid}")]
    public async Task<IActionResult> Get(Guid quizId, [FromQuery] string? difficulty, CancellationToken ct)
    {
        var quiz = await _quizzes.GetAsync(quizId, _currentUser.UserId, difficulty, ct);
        return quiz is null ? NotFound() : Ok(quiz);
    }

    [Authorize]
    [HttpPost("quizzes/{quizId:guid}/attempts/start")]
    public async Task<IActionResult> StartAttempt(Guid quizId, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();

        var result = await _quizzes.StartAttemptAsync(quizId, userId.Value, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [Authorize]
    [HttpPost("quizzes/{quizId:guid}/attempts")]
    public async Task<IActionResult> Submit(Guid quizId, [FromBody] SubmitAttemptRequest request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();

        var result = await _quizzes.SubmitAsync(quizId, userId.Value, request, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [Authorize]
    [HttpGet("quizzes/{quizId:guid}/attempts/result")]
    public async Task<IActionResult> GetAttemptResult(Guid quizId, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();

        var result = await _quizzes.GetAttemptResultAsync(quizId, userId.Value, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}
