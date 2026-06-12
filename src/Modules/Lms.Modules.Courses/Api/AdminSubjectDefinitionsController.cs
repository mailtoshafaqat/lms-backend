using Lms.Modules.Courses.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Modules.Courses.Api;

[ApiController]
[Route("api/v1/admin/subject-definitions")]
[Authorize(Policy = "InstituteAdmin")]
public sealed class AdminSubjectDefinitionsController : ControllerBase
{
    private readonly ISubjectDefinitionService _definitions;

    public AdminSubjectDefinitionsController(ISubjectDefinitionService definitions) =>
        _definitions = definitions;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool activeOnly = false, CancellationToken ct = default) =>
        Ok(await _definitions.ListAsync(activeOnly, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct = default)
    {
        var item = await _definitions.GetAsync(id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSubjectDefinitionRequest req, CancellationToken ct)
    {
        var result = await _definitions.CreateAsync(req, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateSubjectDefinitionRequest req, CancellationToken ct)
    {
        var result = await _definitions.UpdateAsync(id, req, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/archive")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken ct)
    {
        var result = await _definitions.ArchiveAsync(id, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpGet("{id:guid}/library-units")]
    public async Task<IActionResult> ListLibraryUnits(Guid id, CancellationToken ct) =>
        Ok(await _definitions.ListLibraryUnitsAsync(id, ct));

    [HttpPost("{id:guid}/library-units")]
    public async Task<IActionResult> CreateLibraryUnit(
        Guid id, [FromBody] CreateLibraryUnitRequest req, CancellationToken ct)
    {
        var result = await _definitions.CreateLibraryUnitAsync(id, req, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("subjects/{subjectId:guid}/link-shared-units")]
    public async Task<IActionResult> LinkSharedUnits(
        Guid subjectId, [FromBody] LinkSharedUnitsRequest req, CancellationToken ct)
    {
        var result = await _definitions.LinkSharedUnitsToSubjectAsync(subjectId, req, ct);
        return result.Succeeded ? Ok(new { linked = true }) : BadRequest(new { error = result.Error });
    }
}
