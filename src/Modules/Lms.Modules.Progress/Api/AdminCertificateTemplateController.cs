using Lms.Modules.Progress.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Modules.Progress.Api;

[ApiController]
[Route("api/v1/admin/certificate-template")]
[Authorize(Policy = "Teacher")]
public sealed class AdminCertificateTemplateController : ControllerBase
{
    private readonly ICertificateTemplateService _templates;

    public AdminCertificateTemplateController(ICertificateTemplateService templates) => _templates = templates;

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) =>
        Ok(await _templates.GetAsync(ct));

    [HttpPut]
    public async Task<IActionResult> Save(
        [FromBody] UpdateCertificateTemplateRequest request, CancellationToken ct) =>
        Ok(await _templates.SaveAsync(request, ct));
}
