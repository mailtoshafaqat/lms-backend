using Lms.Modules.Assessments.Application;
using Lms.Shared.Auth;
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
    public async Task<IActionResult> GetByTopic(Guid topicId, CancellationToken ct)
    {
        var quiz = await _quizzes.GetByTopicAsync(topicId, ct);
        return quiz is null ? NotFound() : Ok(quiz);
    }

    [HttpGet("quizzes/{quizId:guid}")]
    public async Task<IActionResult> Get(Guid quizId, CancellationToken ct)
    {
        var quiz = await _quizzes.GetAsync(quizId, ct);
        return quiz is null ? NotFound() : Ok(quiz);
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
}
