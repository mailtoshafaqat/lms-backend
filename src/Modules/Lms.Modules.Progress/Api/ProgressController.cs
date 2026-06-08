using Lms.Modules.Progress.Application;
using Lms.Shared.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Modules.Progress.Api;

[ApiController]
[Route("api/v1")]
public sealed class ProgressController : ControllerBase
{
    private readonly IProgressService _progress;
    private readonly IMistakeDiaryService _mistakes;
    private readonly ICurrentUser _currentUser;

    public ProgressController(IProgressService progress, IMistakeDiaryService mistakes, ICurrentUser currentUser)
    {
        _progress = progress;
        _mistakes = mistakes;
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
    [HttpGet("leaderboard")]
    public async Task<IActionResult> Leaderboard([FromQuery] int take = 10, CancellationToken ct = default)
    {
        var rows = await _progress.GetLeaderboardAsync(take, _currentUser.UserId, ct);
        return Ok(rows);
    }

    [Authorize]
    [HttpGet("me/mistakes")]
    public async Task<IActionResult> MyMistakes(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();
        return Ok(await _mistakes.ListAsync(userId.Value, ct: ct));
    }

    [Authorize]
    [HttpPost("me/mistakes/{id:guid}/resolve")]
    public async Task<IActionResult> ResolveMistake(Guid id, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();
        return await _mistakes.ResolveAsync(userId.Value, id, ct) ? Ok(new { resolved = true }) : NotFound();
    }
}
