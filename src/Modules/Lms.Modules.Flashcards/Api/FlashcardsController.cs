using Lms.Modules.Flashcards.Application;
using Lms.Shared.Auth;
using Lms.Shared.Courses;
using Lms.Shared.Enrollments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Modules.Flashcards.Api;

[ApiController]
[Route("api/v1")]
[Authorize]
public sealed class FlashcardsController : ControllerBase
{
    private readonly IFlashcardService _flashcards;
    private readonly ICourseScopeReader _scope;
    private readonly IEnrollmentAccessGuard _enrollment;
    private readonly ICurrentUser _currentUser;

    public FlashcardsController(
        IFlashcardService flashcards,
        ICourseScopeReader scope,
        IEnrollmentAccessGuard enrollment,
        ICurrentUser currentUser)
    {
        _flashcards = flashcards;
        _scope = scope;
        _enrollment = enrollment;
        _currentUser = currentUser;
    }

    [HttpGet("topics/{topicId:guid}/flashcards")]
    public async Task<IActionResult> GetByTopic(Guid topicId, CancellationToken ct)
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

        return Ok(await _flashcards.GetByTopicAsync(topicId, ct));
    }
}
