using Lms.Modules.Assessments.Application;
using Lms.Shared.Auth;
using Lms.Shared.Courses;
using Lms.Shared.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Modules.Assessments.Api;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Policy = "Teacher")]
[RequireProductModule(ProductModule.MockExams)]
public sealed class AdminMockExamsController : ControllerBase
{
    private readonly IMockExamAdminService _mockExams;
    private readonly ISubjectAccessService _access;
    private readonly ICurrentUser _current;

    public AdminMockExamsController(
        IMockExamAdminService mockExams, ISubjectAccessService access, ICurrentUser current)
    {
        _mockExams = mockExams;
        _access = access;
        _current = current;
    }

    [HttpGet("subjects/{subjectId:guid}/mock-exams")]
    public async Task<IActionResult> List(
        Guid subjectId, [FromQuery] bool includeArchived = false, CancellationToken ct = default)
    {
        if (!await CanManageSubject(subjectId, ct)) return Forbid();
        return Ok(await _mockExams.ListForSubjectAsync(subjectId, includeArchived, ct));
    }

    [HttpGet("bundles/{bundleId:guid}/mock-exams")]
    public async Task<IActionResult> ListForBundle(
        Guid bundleId, [FromQuery] bool includeArchived = false, CancellationToken ct = default) =>
        Ok(await _mockExams.ListForBundleAsync(bundleId, includeArchived, ct));

    [HttpGet("mock-exams/{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var exam = await _mockExams.GetAsync(id, ct);
        if (exam is null) return NotFound();
        if (!await CanManageSubject(exam.SubjectId, ct)) return Forbid();
        return Ok(exam);
    }

    [HttpPost("mock-exams")]
    public async Task<IActionResult> Create([FromBody] CreateMockExamRequest req, CancellationToken ct)
    {
        if (!await CanManageSubject(req.SubjectId, ct)) return Forbid();
        var result = await _mockExams.CreateAsync(req, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPut("mock-exams/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMockExamRequest req, CancellationToken ct)
    {
        var existing = await _mockExams.GetAsync(id, ct);
        if (existing is null) return NotFound();
        if (!await CanManageSubject(existing.SubjectId, ct)) return Forbid();

        var result = await _mockExams.UpdateAsync(id, req, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPut("mock-exams/{id:guid}/publish-results")]
    public async Task<IActionResult> PublishResults(Guid id, CancellationToken ct)
    {
        var existing = await _mockExams.GetAsync(id, ct);
        if (existing is null) return NotFound();
        if (!await CanManageSubject(existing.SubjectId, ct)) return Forbid();

        var result = await _mockExams.PublishResultsAsync(id, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPut("mock-exams/{id:guid}/archive")]
    public async Task<IActionResult> SetArchived(
        Guid id, [FromBody] SetMockExamArchivedRequest req, CancellationToken ct)
    {
        var existing = await _mockExams.GetAsync(id, ct);
        if (existing is null) return NotFound();
        if (!await CanManageSubject(existing.SubjectId, ct)) return Forbid();

        var result = await _mockExams.SetArchivedAsync(id, req.IsArchived, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpGet("mock-exams/{id:guid}/leaderboard")]
    public async Task<IActionResult> Leaderboard(Guid id, [FromQuery] int take = 100, CancellationToken ct = default)
    {
        var existing = await _mockExams.GetAsync(id, ct);
        if (existing is null) return NotFound();
        if (!await CanManageSubject(existing.SubjectId, ct)) return Forbid();

        var result = await _mockExams.GetLeaderboardAsync(id, _current.UserId, take, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpDelete("mock-exams/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var existing = await _mockExams.GetAsync(id, ct);
        if (existing is null) return NotFound();
        if (!await CanManageSubject(existing.SubjectId, ct)) return Forbid();
        return await _mockExams.DeleteAsync(id, ct) ? NoContent() : NotFound();
    }

    private Task<bool> CanManageSubject(Guid subjectId, CancellationToken ct) =>
        _access.CanManageSubjectAsync(_current.UserId ?? Guid.Empty, _current.Role ?? Roles.Student, subjectId, ct);
}
