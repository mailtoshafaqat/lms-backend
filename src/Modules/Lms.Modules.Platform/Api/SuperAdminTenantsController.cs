using Lms.Modules.Platform.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Modules.Platform.Api;

[ApiController]
[Route("api/v1/superadmin/tenants")]
[Authorize(Policy = "SuperAdmin")]
public sealed class SuperAdminTenantsController : ControllerBase
{
    private readonly ITenantAdminService _tenants;
    private readonly IPlatformSettingsService _settings;

    public SuperAdminTenantsController(ITenantAdminService tenants, IPlatformSettingsService settings)
    {
        _tenants = tenants;
        _settings = settings;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await _tenants.ListAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var t = await _tenants.GetAsync(id, ct);
        return t is null ? NotFound() : Ok(t);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTenantRequest req, CancellationToken ct)
    {
        var result = await _tenants.CreateAsync(req, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:guid}/flags")]
    public async Task<IActionResult> UpdateFlags(Guid id, [FromBody] UpdateTenantFlagsRequest req, CancellationToken ct)
    {
        var result = await _tenants.UpdateFlagsAsync(id, req, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/admins")]
    public async Task<IActionResult> CreateAdmin(Guid id, [FromBody] CreateTenantAdminRequest req, CancellationToken ct)
    {
        var result = await _tenants.CreateInstituteAdminAsync(id, req, ct);
        return result.Succeeded ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpGet("{id:guid}/branding")]
    public async Task<IActionResult> GetBranding(Guid id, CancellationToken ct)
    {
        var t = await _tenants.GetAsync(id, ct);
        if (t is null) return NotFound();
        var branding = await _settings.GetPublicBrandingAsync(t.Slug, ct);
        return branding is null ? NotFound() : Ok(branding);
    }

    [HttpPut("{id:guid}/branding")]
    public async Task<IActionResult> UpdateBranding(Guid id, [FromBody] UpdateBrandingRequest req, CancellationToken ct) =>
        Ok(await _settings.UpdateTenantBrandingAsync(id, req, ct));
}
