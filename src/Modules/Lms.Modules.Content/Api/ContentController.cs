using Lms.Modules.Content.Application;
using Lms.Shared.Auth;
using Lms.Shared.Courses;
using Lms.Shared.Enrollments;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Modules.Content.Api;

[ApiController]
[Route("api/v1")]
public sealed class ContentController : ControllerBase
{
    private readonly IContentService _content;
    private readonly ICourseScopeReader _scope;
    private readonly IEnrollmentAccessGuard _enrollment;
    private readonly ICurrentUser _currentUser;

    public ContentController(
        IContentService content,
        ICourseScopeReader scope,
        IEnrollmentAccessGuard enrollment,
        ICurrentUser currentUser)
    {
        _content = content;
        _scope = scope;
        _enrollment = enrollment;
        _currentUser = currentUser;
    }

    [HttpGet("topics/{topicId:guid}/content")]
    public async Task<IActionResult> GetTopicContent(Guid topicId, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        var role = _currentUser.Role;

        if (userId is not null && role == Roles.Student)
        {
            var topicScope = await _scope.GetTopicScopeAsync(topicId, ct);
            if (topicScope is not null
                && !await _enrollment.HasBundleAccessAsync(userId, role, topicScope.BundleId, ct))
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { error = "You are not enrolled in this course." });
            }
        }

        return Ok(await _content.GetTopicContentAsync(topicId, ct));
    }
}
