using Lms.Modules.Platform.Application;
using Lms.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Modules.Platform.Api;

[ApiController]
[Authorize(Policy = "PlatformStaff")]
[Route("api/v1/support/incidents")]
public sealed class SupportIncidentsController : ControllerBase
{
    private readonly IRequestIncidentService _incidents;

    public SupportIncidentsController(IRequestIncidentService incidents) => _incidents = incidents;

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? traceId,
        [FromQuery] PagedListQuery paging,
        CancellationToken ct = default)
    {
        var result = await _incidents.SearchAsync(traceId, paging, ct);
        return Ok(result);
    }
}
