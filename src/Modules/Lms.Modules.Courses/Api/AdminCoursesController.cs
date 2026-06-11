using Lms.Modules.Courses.Application;
using Lms.Shared.Auth;
using Lms.Shared.Courses;
using Lms.Shared.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Modules.Courses.Api;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Policy = "Teacher")]
public sealed class AdminCoursesController : ControllerBase
{
    private readonly ICourseAdminService _admin;
    private readonly ISubjectAccessService _access;
    private readonly ICurrentUser _current;
    private readonly ITenantFeaturesProvider _features;
    private readonly ITenantContext _tenant;

    public AdminCoursesController(
        ICourseAdminService admin,
        ISubjectAccessService access,
        ICurrentUser current,
        ITenantFeaturesProvider features,
        ITenantContext tenant)
    {
        _admin = admin;
        _access = access;
        _current = current;
        _features = features;
        _tenant = tenant;
    }

    [HttpPost("bundles")]
    [Authorize(Policy = "InstituteAdmin")]
    public async Task<IActionResult> CreateBundle([FromBody] CreateBundleRequest req, CancellationToken ct) =>
        Ok(await _admin.CreateBundleAsync(req, ct));

    [HttpPut("bundles/{id:guid}")]
    [Authorize(Policy = "InstituteAdmin")]
    public async Task<IActionResult> UpdateBundle(Guid id, [FromBody] UpdateBundleRequest req, CancellationToken ct)
    {
        var flags = await _features.GetAsync(_tenant.TenantId, ct);
        if (flags is not null && !flags.BundlePriceEditEnabled)
            return BadRequest(new { error = "Bundle price editing is disabled for this institute." });

        var r = await _admin.UpdateBundleAsync(id, req, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(new { error = r.Error });
    }

    [HttpPost("bundles/{bundleId:guid}/subjects")]
    [Authorize(Policy = "InstituteAdmin")]
    public async Task<IActionResult> CreateSubject(Guid bundleId, [FromBody] CreateSubjectRequest req, CancellationToken ct)
    {
        var r = await _admin.CreateSubjectAsync(bundleId, req, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(new { error = r.Error });
    }

    [HttpPost("subjects/{subjectId:guid}/units")]
    public async Task<IActionResult> CreateUnit(Guid subjectId, [FromBody] CreateUnitRequest req, CancellationToken ct)
    {
        if (!await CanManageSubject(subjectId, ct)) return Forbid();
        var r = await _admin.CreateUnitAsync(subjectId, req, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(new { error = r.Error });
    }

    [HttpPost("units/{unitId:guid}/topics")]
    public async Task<IActionResult> CreateTopic(Guid unitId, [FromBody] CreateTopicRequest req, CancellationToken ct)
    {
        if (!await CanManageUnit(unitId, ct)) return Forbid();
        var r = await _admin.CreateTopicAsync(unitId, req, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(new { error = r.Error });
    }

    [HttpGet("topics/{id:guid}")]
    public async Task<IActionResult> GetTopic(Guid id, CancellationToken ct)
    {
        if (!await CanManageTopic(id, ct)) return Forbid();
        var r = await _admin.GetTopicAsync(id, ct);
        return r.Succeeded ? Ok(r.Value) : NotFound(new { error = r.Error });
    }

    [HttpPut("topics/{id:guid}")]
    [Authorize(Policy = "InstituteAdmin")]
    public async Task<IActionResult> UpdateTopic(Guid id, [FromBody] UpdateTopicRequest req, CancellationToken ct)
    {
        var r = await _admin.UpdateTopicAsync(id, req, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(new { error = r.Error });
    }

    [HttpPut("subjects/{id:guid}")]
    [Authorize(Policy = "InstituteAdmin")]
    public async Task<IActionResult> UpdateSubject(Guid id, [FromBody] UpdateSubjectRequest req, CancellationToken ct)
    {
        var r = await _admin.UpdateSubjectAsync(id, req, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(new { error = r.Error });
    }

    [HttpPut("units/{id:guid}")]
    [Authorize(Policy = "InstituteAdmin")]
    public async Task<IActionResult> UpdateUnit(Guid id, [FromBody] UpdateUnitRequest req, CancellationToken ct)
    {
        var r = await _admin.UpdateUnitAsync(id, req, ct);
        return r.Succeeded ? Ok(r.Value) : BadRequest(new { error = r.Error });
    }

    [HttpDelete("bundles/{id:guid}")]
    [Authorize(Policy = "InstituteAdmin")]
    public async Task<IActionResult> DeleteBundle(Guid id, CancellationToken ct) =>
        await _admin.DeleteBundleAsync(id, ct) ? NoContent() : NotFound();

    [HttpDelete("subjects/{id:guid}")]
    [Authorize(Policy = "InstituteAdmin")]
    public async Task<IActionResult> DeleteSubject(Guid id, CancellationToken ct) =>
        await _admin.DeleteSubjectAsync(id, ct) ? NoContent() : NotFound();

    [HttpDelete("units/{id:guid}")]
    public async Task<IActionResult> DeleteUnit(Guid id, CancellationToken ct)
    {
        if (!await CanManageUnit(id, ct)) return Forbid();
        return await _admin.DeleteUnitAsync(id, ct) ? NoContent() : NotFound();
    }

    [HttpDelete("topics/{id:guid}")]
    public async Task<IActionResult> DeleteTopic(Guid id, CancellationToken ct)
    {
        if (!await CanManageTopic(id, ct)) return Forbid();
        return await _admin.DeleteTopicAsync(id, ct) ? NoContent() : NotFound();
    }

    private Guid UserId => _current.UserId ?? Guid.Empty;
    private string Role => _current.Role ?? Roles.Student;

    private Task<bool> CanManageSubject(Guid subjectId, CancellationToken ct) =>
        _access.CanManageSubjectAsync(UserId, Role, subjectId, ct);

    private Task<bool> CanManageUnit(Guid unitId, CancellationToken ct) =>
        _access.CanManageUnitAsync(UserId, Role, unitId, ct);

    private Task<bool> CanManageTopic(Guid topicId, CancellationToken ct) =>
        _access.CanManageTopicAsync(UserId, Role, topicId, ct);
}
