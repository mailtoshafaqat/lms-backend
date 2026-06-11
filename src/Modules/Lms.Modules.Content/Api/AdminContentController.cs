using Lms.Modules.Content.Application;
using Lms.Shared.Auth;
using Lms.Shared.Courses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Modules.Content.Api;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Policy = "Teacher")]
public sealed class AdminContentController : ControllerBase
{
    private readonly IContentAdminService _admin;
    private readonly IContentService _content;
    private readonly ISubjectAccessService _access;
    private readonly ICurrentUser _current;

    public AdminContentController(
        IContentAdminService admin,
        IContentService content,
        ISubjectAccessService access,
        ICurrentUser current)
    {
        _admin = admin;
        _content = content;
        _access = access;
        _current = current;
    }

    [HttpGet("topics/{topicId:guid}/content")]
    public async Task<IActionResult> GetContent(Guid topicId, CancellationToken ct)
    {
        if (!await CanManageTopic(topicId, ct)) return TopicAccessDenied();
        return Ok(await _content.GetTopicContentAsync(topicId, ct));
    }

    [HttpPost("topics/{topicId:guid}/lectures")]
    public async Task<IActionResult> AddLecture(Guid topicId, [FromBody] CreateLectureRequest req, CancellationToken ct)
    {
        if (!await CanManageTopic(topicId, ct)) return TopicAccessDenied();
        return Ok(await _admin.AddLectureAsync(topicId, req, ct));
    }

    [HttpPost("topics/{topicId:guid}/notes")]
    public async Task<IActionResult> AddNote(Guid topicId, [FromBody] CreateNoteRequest req, CancellationToken ct)
    {
        if (!await CanManageTopic(topicId, ct)) return TopicAccessDenied();
        return Ok(await _admin.AddNoteAsync(topicId, req, ct));
    }

    [HttpDelete("lectures/{id:guid}")]
    public async Task<IActionResult> DeleteLecture(Guid id, CancellationToken ct) =>
        await _admin.DeleteLectureAsync(id, ct) ? NoContent() : NotFound();

    [HttpDelete("notes/{id:guid}")]
    public async Task<IActionResult> DeleteNote(Guid id, CancellationToken ct) =>
        await _admin.DeleteNoteAsync(id, ct) ? NoContent() : NotFound();

    private Task<bool> CanManageTopic(Guid topicId, CancellationToken ct) =>
        _access.CanManageTopicAsync(
            _current.UserId ?? Guid.Empty,
            _current.Role ?? string.Empty,
            topicId,
            ct);

    private static IActionResult TopicAccessDenied() =>
        new ObjectResult(new { error = "You do not have access to this topic. Institute admins can manage all subjects; teachers need an assignment for this subject." })
        {
            StatusCode = StatusCodes.Status403Forbidden
        };
}
