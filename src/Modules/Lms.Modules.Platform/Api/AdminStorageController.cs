using Lms.Shared.Storage;
using Lms.Shared.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Modules.Platform.Api;

[ApiController]
[Route("api/v1/admin/storage")]
[Authorize(Policy = "Teacher")]
public sealed class AdminStorageController : ControllerBase
{
    private readonly ITenantStorageQuotaService _storage;
    private readonly ITenantContext _tenant;

    public AdminStorageController(ITenantStorageQuotaService storage, ITenantContext tenant)
    {
        _storage = storage;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsage(CancellationToken ct) =>
        Ok(await _storage.GetUsageAsync(_tenant.TenantId, ct));
}
