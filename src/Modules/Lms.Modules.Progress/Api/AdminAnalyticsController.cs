using Lms.Modules.Progress.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Modules.Progress.Api;

[ApiController]
[Route("api/v1/admin/analytics")]
[Authorize(Policy = "Teacher")]
public sealed class AdminAnalyticsController : ControllerBase
{
    private readonly ICohortAnalyticsService _analytics;

    public AdminAnalyticsController(ICohortAnalyticsService analytics) => _analytics = analytics;

    [HttpGet("cohort")]
    public async Task<IActionResult> CohortOverview(
        [FromQuery] Guid bundleId,
        [FromQuery] Guid? subjectId,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        CancellationToken ct = default)
    {
        var overview = await _analytics.GetOverviewAsync(bundleId, subjectId, fromUtc, toUtc, ct);
        if (!overview.Succeeded)
            return overview.Error == "Bundle not found or has no topics."
                ? NotFound(new { error = overview.Error })
                : BadRequest(new { error = overview.Error });
        return Ok(overview.Value);
    }

    [HttpGet("cohort/students")]
    public async Task<IActionResult> CohortStudents(
        [FromQuery] Guid bundleId,
        [FromQuery] Guid? subjectId,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        CancellationToken ct = default)
    {
        var rows = await _analytics.GetStudentRowsAsync(bundleId, subjectId, fromUtc, toUtc, ct);
        if (!rows.Succeeded)
            return rows.Error == "Bundle not found or has no topics."
                ? NotFound(new { error = rows.Error })
                : BadRequest(new { error = rows.Error });
        return Ok(rows.Value);
    }

    [HttpGet("cohort/export")]
    public async Task<IActionResult> ExportCohortCsv(
        [FromQuery] Guid bundleId,
        [FromQuery] Guid? subjectId,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        CancellationToken ct = default)
    {
        var result = await _analytics.ExportCsvAsync(bundleId, subjectId, fromUtc, toUtc, ct);
        if (!result.Succeeded)
            return result.Error == "Bundle not found or has no topics."
                ? NotFound(new { error = result.Error })
                : BadRequest(new { error = result.Error });

        var fileName = $"cohort-analytics-{bundleId:N}.csv";
        return File(result.Value!, "text/csv", fileName);
    }
}
