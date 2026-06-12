using Lms.Modules.Progress.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Modules.Progress.Api;

[ApiController]
[Route("api/v1/admin/certificates")]
[Authorize(Policy = "Teacher")]
public sealed class AdminCertificatesController : ControllerBase
{
    private readonly ICertificateService _certificates;

    public AdminCertificatesController(ICertificateService certificates) => _certificates = certificates;

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? bundleId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default) =>
        Ok(await _certificates.ListAdminAsync(bundleId, page, pageSize, ct));

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> DownloadPdf(Guid id, CancellationToken ct)
    {
        var pdf = await _certificates.GetPdfForAdminAsync(id, ct);
        if (pdf is null) return NotFound();
        return File(pdf, "application/pdf", $"certificate-{id:N}.pdf");
    }
}
