using Lms.Modules.Platform.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Platform.Api;

[ApiController]
[Route("api/v1/admin/settings")]
[Authorize(Policy = "InstituteAdmin")]
public sealed class AdminSettingsController : ControllerBase
{
    private readonly IPlatformSettingsService _settings;
    private readonly ILandingPageService _landing;

    public AdminSettingsController(IPlatformSettingsService settings, ILandingPageService landing)
    {
        _settings = settings;
        _landing = landing;
    }

    [HttpGet("email")]
    public async Task<IActionResult> GetEmail(CancellationToken ct) =>
        Ok(await _settings.GetEmailSettingsAsync(ct));

    [HttpPut("email")]
    public async Task<IActionResult> UpdateEmail([FromBody] UpdateEmailSettingsRequest req, CancellationToken ct) =>
        Ok(await _settings.UpdateEmailSettingsAsync(req, ct));

    [HttpGet("zoom")]
    public async Task<IActionResult> GetZoom(CancellationToken ct) =>
        Ok(await _settings.GetZoomSettingsAsync(ct));

    [HttpPut("zoom")]
    public async Task<IActionResult> UpdateZoom([FromBody] UpdateZoomSettingsRequest req, CancellationToken ct) =>
        Ok(await _settings.UpdateZoomSettingsAsync(req, ct));

    [HttpGet("branding")]
    public async Task<IActionResult> GetBranding(CancellationToken ct) =>
        Ok(await _settings.GetBrandingAsync(ct));

    [HttpPut("branding")]
    public async Task<IActionResult> UpdateBranding([FromBody] UpdateBrandingRequest req, CancellationToken ct) =>
        Ok(await _settings.UpdateBrandingAsync(req, ct));

    [HttpGet("landing")]
    public async Task<IActionResult> GetLanding(CancellationToken ct) =>
        Ok(await _landing.GetAdminAsync(ct));

    [HttpPut("landing")]
    public async Task<IActionResult> UpdateLanding([FromBody] UpdateLandingPageRequest req, CancellationToken ct)
    {
        try
        {
            return Ok(await _landing.UpdateAdminAsync(req, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { error = "Landing page was changed elsewhere. Refresh and try again." });
        }
    }
}
