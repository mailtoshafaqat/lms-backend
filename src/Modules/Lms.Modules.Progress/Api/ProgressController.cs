using Lms.Modules.Progress.Application;
using Lms.Shared.Auth;
using Lms.Shared.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Modules.Progress.Api;

[ApiController]
[Route("api/v1")]
public sealed class ProgressController : ControllerBase
{
    private readonly IProgressService _progress;
    private readonly IMistakeDiaryService _mistakes;
    private readonly IBookmarkService _bookmarks;
    private readonly IWeaknessQuizService _weaknessQuiz;
    private readonly ICurrentUser _currentUser;

    public ProgressController(
        IProgressService progress,
        IMistakeDiaryService mistakes,
        IBookmarkService bookmarks,
        IWeaknessQuizService weaknessQuiz,
        ICurrentUser currentUser)
    {
        _progress = progress;
        _mistakes = mistakes;
        _bookmarks = bookmarks;
        _weaknessQuiz = weaknessQuiz;
        _currentUser = currentUser;
    }

    [Authorize]
    [HttpGet("me/grades")]
    public async Task<IActionResult> MyGrades(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();
        return Ok(await _progress.GetMyGradesAsync(userId.Value, ct));
    }

    [Authorize]
    [HttpGet("me/dashboard")]
    public async Task<IActionResult> DashboardOverview(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();
        return Ok(await _progress.GetDashboardOverviewAsync(userId.Value, ct));
    }

    [Authorize]
    [HttpGet("leaderboard")]
    public async Task<IActionResult> Leaderboard([FromQuery] int take = 10, CancellationToken ct = default)
    {
        var size = take <= 5 ? 5 : 10;
        var rows = await _progress.GetLeaderboardAsync(size, _currentUser.UserId, ct);
        return Ok(rows);
    }

    [Authorize]
    [HttpGet("me/mistakes")]
    [RequireProductModule(ProductModule.MistakeDiary)]
    public async Task<IActionResult> MyMistakes(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();
        return Ok(await _mistakes.ListAsync(userId.Value, ct: ct));
    }

    [Authorize]
    [HttpPost("me/mistakes/{id:guid}/resolve")]
    [RequireProductModule(ProductModule.MistakeDiary)]
    public async Task<IActionResult> ResolveMistake(Guid id, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();
        return await _mistakes.ResolveAsync(userId.Value, id, ct) ? Ok(new { resolved = true }) : NotFound();
    }

    [Authorize]
    [HttpGet("me/bookmarks")]
    public async Task<IActionResult> MyBookmarks(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();
        return Ok(await _bookmarks.ListAsync(userId.Value, ct));
    }

    [Authorize]
    [HttpPost("me/bookmarks")]
    public async Task<IActionResult> CreateBookmark([FromBody] CreateBookmarkRequest request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();
        try
        {
            return Ok(await _bookmarks.CreateAsync(userId.Value, request, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [Authorize]
    [HttpDelete("me/bookmarks/{id:guid}")]
    public async Task<IActionResult> DeleteBookmark(Guid id, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();
        return await _bookmarks.DeleteAsync(userId.Value, id, ct) ? NoContent() : NotFound();
    }

    [Authorize]
    [HttpGet("me/bookmarks/status")]
    public async Task<IActionResult> BookmarkStatus(
        [FromQuery] string targetType,
        [FromQuery] Guid targetId,
        CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();
        return Ok(await _bookmarks.GetStatusAsync(userId.Value, targetType, targetId, ct));
    }

    [Authorize]
    [HttpGet("me/weakness-quiz")]
    [RequireProductModule(ProductModule.MistakeDiary)]
    public async Task<IActionResult> WeaknessQuiz([FromQuery] int count = 10, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();
        var quiz = await _weaknessQuiz.BuildAsync(userId.Value, count, ct);
        return quiz is null
            ? NotFound(new { error = "No weakness questions yet. Take topic quizzes or review your mistake diary first." })
            : Ok(quiz);
    }

    [Authorize]
    [HttpPost("me/weakness-quiz/submit")]
    [RequireProductModule(ProductModule.MistakeDiary)]
    public async Task<IActionResult> SubmitWeaknessQuiz(
        [FromBody] SubmitWeaknessQuizRequest request,
        CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();
        try
        {
            return Ok(await _weaknessQuiz.SubmitAsync(userId.Value, request, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
