using Lms.Modules.Courses.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Modules.Courses.Api;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Policy = "Teacher")]
public sealed class AdminCoursesController : ControllerBase
{
    private readonly ICourseAdminService _admin;

    public AdminCoursesController(ICourseAdminService admin) => _admin = admin;

    [HttpPost("bundles")]
    public async Task<IActionResult> CreateBundle([FromBody] CreateBundleRequest req, CancellationToken ct) =>
        Ok(await _admin.CreateBundleAsync(req, ct));

    [HttpPost("bundles/{bundleId:guid}/subjects")]
    public async Task<IActionResult> CreateSubject(Guid bundleId, [FromBody] CreateSubjectRequest req, CancellationToken ct)
    {
        var r = await _admin.CreateSubjectAsync(bundleId, req, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(new { error = r.Error });
    }

    [HttpPost("subjects/{subjectId:guid}/units")]
    public async Task<IActionResult> CreateUnit(Guid subjectId, [FromBody] CreateUnitRequest req, CancellationToken ct)
    {
        var r = await _admin.CreateUnitAsync(subjectId, req, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(new { error = r.Error });
    }

    [HttpPost("units/{unitId:guid}/topics")]
    public async Task<IActionResult> CreateTopic(Guid unitId, [FromBody] CreateTopicRequest req, CancellationToken ct)
    {
        var r = await _admin.CreateTopicAsync(unitId, req, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(new { error = r.Error });
    }

    [HttpDelete("bundles/{id:guid}")]
    public async Task<IActionResult> DeleteBundle(Guid id, CancellationToken ct) =>
        await _admin.DeleteBundleAsync(id, ct) ? NoContent() : NotFound();

    [HttpDelete("subjects/{id:guid}")]
    public async Task<IActionResult> DeleteSubject(Guid id, CancellationToken ct) =>
        await _admin.DeleteSubjectAsync(id, ct) ? NoContent() : NotFound();

    [HttpDelete("units/{id:guid}")]
    public async Task<IActionResult> DeleteUnit(Guid id, CancellationToken ct) =>
        await _admin.DeleteUnitAsync(id, ct) ? NoContent() : NotFound();

    [HttpDelete("topics/{id:guid}")]
    public async Task<IActionResult> DeleteTopic(Guid id, CancellationToken ct) =>
        await _admin.DeleteTopicAsync(id, ct) ? NoContent() : NotFound();
}
