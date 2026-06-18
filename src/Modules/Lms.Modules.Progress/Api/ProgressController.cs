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
    private readonly IVideoProgressService _videoProgress;
    private readonly ICertificateService _certificates;
    private readonly IStudentProgramService _program;
    private readonly IStudentStatsService _stats;
    private readonly IStudentNotificationQueryService _notifications;
    private readonly ICurrentUser _currentUser;

    public ProgressController(
        IProgressService progress,
        IMistakeDiaryService mistakes,
        IBookmarkService bookmarks,
        IWeaknessQuizService weaknessQuiz,
        IVideoProgressService videoProgress,
        ICertificateService certificates,
        IStudentProgramService program,
        IStudentStatsService stats,
        IStudentNotificationQueryService notifications,
        ICurrentUser currentUser)
    {
        _progress = progress;
        _mistakes = mistakes;
        _bookmarks = bookmarks;
        _weaknessQuiz = weaknessQuiz;
        _videoProgress = videoProgress;
        _certificates = certificates;
        _program = program;
        _stats = stats;
        _notifications = notifications;
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
    [HttpGet("me/program")]
    public async Task<IActionResult> MyProgram(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();
        return Ok(await _program.GetMyProgramAsync(userId.Value, ct));
    }

    [Authorize]
    [HttpGet("me/stats")]
    public async Task<IActionResult> MyStats(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();
        return Ok(await _stats.GetMyStatsAsync(userId.Value, ct));
    }

    [Authorize]
    [HttpGet("me/notifications")]
    public async Task<IActionResult> MyNotifications(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();
        var items = await _notifications.ListAsync(userId.Value, ct);
        var unread = await _notifications.GetUnreadCountAsync(userId.Value, ct);
        return Ok(new { unreadCount = unread, items });
    }

    [Authorize]
    [HttpPost("me/notifications/{id:guid}/read")]
    public async Task<IActionResult> MarkNotificationRead(Guid id, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();
        return await _notifications.MarkReadAsync(userId.Value, id, ct)
            ? Ok(new { read = true })
            : NotFound();
    }

    [Authorize]
    [HttpPost("me/notifications/read-all")]
    public async Task<IActionResult> MarkAllNotificationsRead(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();
        var count = await _notifications.MarkAllReadAsync(userId.Value, ct);
        return Ok(new { marked = count });
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
    [HttpPut("me/lectures/{lectureId:guid}/progress")]
    public async Task<IActionResult> SaveLectureProgress(
        Guid lectureId, [FromBody] SaveLectureProgressRequest request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();
        try
        {
            return Ok(await _videoProgress.SaveProgressAsync(userId.Value, lectureId, request, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [Authorize]
    [HttpGet("me/lectures/{lectureId:guid}/progress")]
    public async Task<IActionResult> GetLectureProgress(Guid lectureId, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();
        var row = await _videoProgress.GetProgressAsync(userId.Value, lectureId, ct);
        return row is null ? NotFound() : Ok(row);
    }

    [Authorize]
    [HttpGet("me/lectures/progress")]
    public async Task<IActionResult> GetLectureProgressBulk(
        [FromQuery] string lectureIds, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();
        var ids = (lectureIds ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => Guid.TryParse(s, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToList();
        return Ok(await _videoProgress.GetProgressForLecturesAsync(userId.Value, ids, ct));
    }

    [Authorize]
    [HttpGet("me/certificates")]
    public async Task<IActionResult> MyCertificates(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();
        return Ok(await _certificates.ListMineAsync(userId.Value, ct));
    }

    [Authorize]
    [HttpGet("me/certificates/{id:guid}/pdf")]
    public async Task<IActionResult> DownloadMyCertificatePdf(Guid id, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Unauthorized();
        var pdf = await _certificates.GetPdfForStudentAsync(id, userId.Value, ct);
        if (pdf is null) return NotFound();
        return File(pdf, "application/pdf", $"certificate-{id:N}.pdf");
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
