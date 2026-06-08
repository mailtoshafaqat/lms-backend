using Lms.Modules.Content.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Modules.Content.Api;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Policy = "Teacher")]
public sealed class AdminContentController : ControllerBase
{
    private readonly IContentAdminService _admin;
    private readonly IContentService _content;

    public AdminContentController(IContentAdminService admin, IContentService content)
    {
        _admin = admin;
        _content = content;
    }

    [HttpGet("topics/{topicId:guid}/content")]
    public async Task<IActionResult> GetContent(Guid topicId, CancellationToken ct) =>
        Ok(await _content.GetTopicContentAsync(topicId, ct));

    [HttpPost("topics/{topicId:guid}/lectures")]
    public async Task<IActionResult> AddLecture(Guid topicId, [FromBody] CreateLectureRequest req, CancellationToken ct) =>
        Ok(await _admin.AddLectureAsync(topicId, req, ct));

    [HttpPost("topics/{topicId:guid}/notes")]
    public async Task<IActionResult> AddNote(Guid topicId, [FromBody] CreateNoteRequest req, CancellationToken ct) =>
        Ok(await _admin.AddNoteAsync(topicId, req, ct));

    [HttpDelete("lectures/{id:guid}")]
    public async Task<IActionResult> DeleteLecture(Guid id, CancellationToken ct) =>
        await _admin.DeleteLectureAsync(id, ct) ? NoContent() : NotFound();

    [HttpDelete("notes/{id:guid}")]
    public async Task<IActionResult> DeleteNote(Guid id, CancellationToken ct) =>
        await _admin.DeleteNoteAsync(id, ct) ? NoContent() : NotFound();
}
