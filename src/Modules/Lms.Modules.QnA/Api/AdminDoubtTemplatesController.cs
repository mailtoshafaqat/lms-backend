using Lms.Modules.QnA.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Modules.QnA.Api;

[ApiController]
[Route("api/v1/admin/doubt-templates")]
[Authorize(Policy = "Teacher")]
public sealed class AdminDoubtTemplatesController : ControllerBase
{
    private readonly IDoubtTemplateService _templates;

    public AdminDoubtTemplatesController(IDoubtTemplateService templates) => _templates = templates;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await _templates.ListAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDoubtReplyTemplateRequest req, CancellationToken ct)
    {
        var result = await _templates.CreateAsync(req, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDoubtReplyTemplateRequest req, CancellationToken ct)
    {
        var result = await _templates.UpdateAsync(id, req, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        await _templates.DeleteAsync(id, ct) ? NoContent() : NotFound();
}
