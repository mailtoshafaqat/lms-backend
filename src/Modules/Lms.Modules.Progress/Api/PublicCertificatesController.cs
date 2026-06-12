using Lms.Modules.Progress.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Modules.Progress.Api;

[ApiController]
[Route("api/v1/public/certificates")]
[AllowAnonymous]
public sealed class PublicCertificatesController : ControllerBase
{
    private readonly ICertificateService _certificates;

    public PublicCertificatesController(ICertificateService certificates) => _certificates = certificates;

    [HttpGet("verify/{certificateNumber}")]
    public async Task<IActionResult> Verify(
        string certificateNumber,
        [FromQuery] string tenant,
        CancellationToken ct)
    {
        var result = await _certificates.VerifyAsync(certificateNumber, tenant ?? "", ct);
        return Ok(result);
    }
}
