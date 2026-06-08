using Lms.Modules.Assessments.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Modules.Assessments.Api;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Policy = "Teacher")]
public sealed class AdminAssessmentsController : ControllerBase
{
    private readonly IQuizAdminService _admin;

    public AdminAssessmentsController(IQuizAdminService admin) => _admin = admin;

    [HttpGet("topics/{topicId:guid}/quiz")]
    public async Task<IActionResult> GetQuiz(Guid topicId, CancellationToken ct)
    {
        var quiz = await _admin.GetAdminQuizAsync(topicId, ct);
        return Ok(quiz);
    }

    [HttpPost("topics/{topicId:guid}/questions")]
    public async Task<IActionResult> AddQuestion(Guid topicId, [FromBody] CreateQuestionRequest req, CancellationToken ct)
    {
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
        var r = await _admin.UpdateQuizTitleAsync(topicId, req, ct);
        return r.Succeeded ? Ok(new { updated = true }) : BadRequest(new { error = r.Error });
    }

    [HttpPut("topics/{topicId:guid}/quiz/reorder")]
    public async Task<IActionResult> ReorderQuestions(Guid topicId, [FromBody] ReorderQuestionsRequest req, CancellationToken ct)
    {
        var r = await _admin.ReorderQuestionsAsync(topicId, req, ct);
        return r.Succeeded ? Ok(new { reordered = true }) : BadRequest(new { error = r.Error });
    }
}
