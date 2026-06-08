using Lms.Modules.Courses.Application;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Modules.Courses.Api;

[ApiController]
[Route("api/v1")]
public sealed class CoursesController : ControllerBase
{
    private readonly ICourseService _courses;

    public CoursesController(ICourseService courses) => _courses = courses;

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
}
