using Lms.Modules.Assessments.Application;
using Lms.Shared.Auth;
using Lms.Shared.Courses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Modules.Assessments.Api;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Policy = "Teacher")]
public sealed class AdminAssessmentsController : ControllerBase
{
    private readonly IQuizAdminService _admin;
    private readonly ISubjectAccessService _access;
    private readonly ICurrentUser _current;

    public AdminAssessmentsController(
        IQuizAdminService admin, ISubjectAccessService access, ICurrentUser current)
    {
        _admin = admin;
        _access = access;
        _current = current;
    }

    [HttpGet("topics/{topicId:guid}/quiz")]
    public async Task<IActionResult> GetQuiz(Guid topicId, CancellationToken ct)
    {
        if (!await CanManageTopic(topicId, ct)) return Forbid();
        var quiz = await _admin.GetAdminQuizAsync(topicId, ct);
        return Ok(quiz);
    }

    [HttpPost("topics/{topicId:guid}/questions")]
    public async Task<IActionResult> AddQuestion(Guid topicId, [FromBody] CreateQuestionRequest req, CancellationToken ct)
    {
        if (!await CanManageTopic(topicId, ct)) return Forbid();
        var r = await _admin.AddQuestionAsync(topicId, req, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(new { error = r.Error });
    }

    [HttpDelete("questions/{id:guid}")]
    public async Task<IActionResult> DeleteQuestion(Guid id, CancellationToken ct) =>
        await _admin.DeleteQuestionAsync(id, ct) ? NoContent() : NotFound();

    [HttpPut("questions/{id:guid}")]
    public async Task<IActionResult> UpdateQuestion(Guid id, [FromBody] UpdateQuestionRequest req, CancellationToken ct)
    {
        var r = await _admin.UpdateQuestionAsync(id, req, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(new { error = r.Error });
    }

    [HttpPut("topics/{topicId:guid}/quiz/title")]
    public async Task<IActionResult> UpdateQuizTitle(Guid topicId, [FromBody] UpdateQuizTitleRequest req, CancellationToken ct)
    {
        if (!await CanManageTopic(topicId, ct)) return Forbid();
        var r = await _admin.UpdateQuizTitleAsync(topicId, req, ct);
        return r.Succeeded ? Ok(new { updated = true }) : BadRequest(new { error = r.Error });
    }

    [HttpPut("topics/{topicId:guid}/quiz/reorder")]
    public async Task<IActionResult> ReorderQuestions(Guid topicId, [FromBody] ReorderQuestionsRequest req, CancellationToken ct)
    {
        if (!await CanManageTopic(topicId, ct)) return Forbid();
        var r = await _admin.ReorderQuestionsAsync(topicId, req, ct);
        return r.Succeeded ? Ok(new { reordered = true }) : BadRequest(new { error = r.Error });
    }

    [HttpPut("topics/{topicId:guid}/quiz/settings")]
    public async Task<IActionResult> UpdateQuizSettings(
        Guid topicId, [FromBody] UpdateQuizSettingsRequest req, CancellationToken ct)
    {
        if (!await CanManageTopic(topicId, ct)) return Forbid();
        var r = await _admin.UpdateQuizSettingsAsync(topicId, req, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(new { error = r.Error });
    }

    [HttpPut("topics/{topicId:guid}/quiz/publish-results")]
    public async Task<IActionResult> PublishQuizResults(Guid topicId, CancellationToken ct)
    {
        if (!await CanManageTopic(topicId, ct)) return Forbid();
        var r = await _admin.PublishResultsAsync(topicId, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(new { error = r.Error });
    }

    [HttpGet("topics/{topicId:guid}/quiz/analytics")]
    public async Task<IActionResult> GetQuizAnalytics(Guid topicId, CancellationToken ct)
    {
        if (!await CanManageTopic(topicId, ct)) return Forbid();
        var analytics = await _admin.GetQuizAnalyticsAsync(topicId, ct);
        return analytics is null ? NotFound() : Ok(analytics);
    }

    [HttpPost("topics/{topicId:guid}/questions/import/preview")]
    public async Task<IActionResult> PreviewMcqImport(
        Guid topicId, [FromBody] McqImportRequest req, CancellationToken ct)
    {
        if (!await CanManageTopic(topicId, ct)) return Forbid();
        var r = await _admin.PreviewMcqImportAsync(req.Rows, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(new { error = r.Error });
    }

    [HttpPost("topics/{topicId:guid}/questions/import")]
    public async Task<IActionResult> ImportMcq(
        Guid topicId, [FromBody] McqImportRequest req, CancellationToken ct)
    {
        if (!await CanManageTopic(topicId, ct)) return Forbid();
        var r = await _admin.ImportMcqAsync(topicId, req, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(new { error = r.Error });
    }

    [HttpGet("units/{unitId:guid}/quizzes/{quizType}")]
    public async Task<IActionResult> GetUnitQuiz(Guid unitId, string quizType, CancellationToken ct)
    {
        if (!await CanManageUnit(unitId, ct)) return Forbid();
        var quiz = await _admin.GetUnitQuizAsync(unitId, quizType, ct);
        return quiz is null ? NotFound() : Ok(quiz);
    }

    [HttpPut("units/{unitId:guid}/quizzes/{quizType}/settings")]
    public async Task<IActionResult> UpdateUnitQuizSettings(
        Guid unitId, string quizType, [FromBody] UpdateQuizSettingsRequest req, CancellationToken ct)
    {
        if (!await CanManageUnit(unitId, ct)) return Forbid();
        var r = await _admin.UpdateUnitQuizSettingsAsync(unitId, quizType, req, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(new { error = r.Error });
    }

    private Task<bool> CanManageTopic(Guid topicId, CancellationToken ct) =>
        _access.CanManageTopicAsync(_current.UserId ?? Guid.Empty, _current.Role ?? Roles.Student, topicId, ct);

    private Task<bool> CanManageUnit(Guid unitId, CancellationToken ct) =>
        _access.CanManageUnitAsync(_current.UserId ?? Guid.Empty, _current.Role ?? Roles.Student, unitId, ct);
}
