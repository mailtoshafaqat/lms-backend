using Lms.Modules.Platform.Application;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Modules.Platform.Api;

[ApiController]
[Route("api/v1/public/landing")]
public sealed class PublicLandingController : ControllerBase
{
    private readonly ILandingPageService _landing;

    public PublicLandingController(ILandingPageService landing) => _landing = landing;

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct)
    {
        var page = await _landing.GetPublicAsync(slug, ct);
        return page is null ? NotFound() : Ok(page);
    }
}
