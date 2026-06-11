using Lms.Modules.Platform.Application;
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
    public async Task<IActionResult> Search([FromQuery] string? traceId, [FromQuery] int take = 25, CancellationToken ct = default)
    {
        var rows = await _incidents.SearchAsync(traceId, take, ct);
        return Ok(rows);
    }
}
