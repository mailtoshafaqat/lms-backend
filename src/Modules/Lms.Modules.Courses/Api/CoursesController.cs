using Lms.Modules.Courses.Application;
using Lms.Shared.Auth;
using Lms.Shared.Courses;
using Lms.Shared.Enrollments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Modules.Courses.Api;

[ApiController]
[Route("api/v1")]
public sealed class CoursesController : ControllerBase
{
    private readonly ICourseService _courses;
    private readonly ICourseContentSearch _search;
    private readonly IEnrollmentReader _enrollments;
    private readonly ITopicNavigationService _topicNav;
    private readonly ICurrentUser _currentUser;

    public CoursesController(
        ICourseService courses,
        ICourseContentSearch search,
        IEnrollmentReader enrollments,
        ITopicNavigationService topicNav,
        ICurrentUser currentUser)
    {
        _courses = courses;
        _search = search;
        _enrollments = enrollments;
        _topicNav = topicNav;
        _currentUser = currentUser;
    }

    [HttpGet("bundles")]
    public async Task<IActionResult> GetBundles(CancellationToken ct) =>
        Ok(await _courses.GetBundlesAsync(ct));

    [HttpGet("bundles/{id:guid}")]
    public async Task<IActionResult> GetBundle(Guid id, CancellationToken ct)
    {
        var bundle = await _courses.GetBundleAsync(id, ct);
        return bundle is null ? NotFound() : Ok(bundle);
    }

    [HttpGet("subjects/{subjectId:guid}/units")]
    public async Task<IActionResult> GetUnits(Guid subjectId, CancellationToken ct) =>
        Ok(await _courses.GetUnitsAsync(subjectId, ct));

    [HttpGet("units/{unitId:guid}/topics")]
    public async Task<IActionResult> GetTopics(Guid unitId, CancellationToken ct) =>
        Ok(await _courses.GetTopicsAsync(unitId, ct));

    [HttpGet("topics/recent")]
    public async Task<IActionResult> GetRecentTopics([FromQuery] int take, CancellationToken ct) =>
        Ok(await _courses.GetRecentTopicsAsync(take <= 0 ? 3 : take, ct));

    [Authorize]
    [HttpGet("topics/{id:guid}/navigation")]
    public async Task<IActionResult> GetTopicNavigation(Guid id, CancellationToken ct)
    {
        var nav = await _topicNav.GetNavigationAsync(id, ct);
        return nav is null ? NotFound() : Ok(nav);
    }

    [Authorize]
    [HttpGet("search")]
    public async Task<IActionResult> SearchContent([FromQuery] string q, [FromQuery] int take = 20, CancellationToken ct = default)
    {
        IReadOnlyList<Guid>? bundleIds = null;
        var userId = _currentUser.UserId;
        if (userId is not null && _currentUser.Role == Roles.Student)
        {
            var enrolled = await _enrollments.GetActiveBundleIdsAsync(userId.Value, ct);
            if (enrolled.Count > 0) bundleIds = enrolled;
        }

        return Ok(await _search.SearchAsync(q, bundleIds, take, ct));
    }
}
