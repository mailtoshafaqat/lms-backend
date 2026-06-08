using Lms.Modules.Platform.Application;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Modules.Platform.Api;

[ApiController]
[Route("api/v1/public/branding")]
public sealed class PublicBrandingController : ControllerBase
{
    private readonly IPlatformSettingsService _settings;

    public PublicBrandingController(IPlatformSettingsService settings) => _settings = settings;

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct)
    {
        var branding = await _settings.GetPublicBrandingAsync(slug, ct);
        return branding is null ? NotFound() : Ok(branding);
    }
}
