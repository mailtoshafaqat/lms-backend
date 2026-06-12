using Lms.Modules.Platform.Domain;
using Lms.Modules.Platform.Infrastructure;
using Lms.Shared.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lms.Modules.Platform.Application;

public sealed class TenantStorageQuotaService : ITenantStorageQuotaService
{
    private const int WarningPercent = 80;

    private readonly PlatformDbContext _db;
    private readonly StorageQuotaOptions _options;

    public TenantStorageQuotaService(PlatformDbContext db, IOptions<StorageQuotaOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public async Task<TenantStorageUsageDto> GetUsageAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId, ct)
            ?? throw new InvalidOperationException("Tenant not found.");
        return MapUsage(tenant);
    }

    public async Task<IReadOnlyList<TenantStorageUsageDto>> ListAllUsageAsync(CancellationToken ct = default)
    {
        var tenants = await _db.Tenants.AsNoTracking().OrderBy(t => t.Name).ToListAsync(ct);
        return tenants.Select(t => MapUsage(t)).ToList();
    }

    public async Task<StorageUploadCheckResult> CheckUploadAsync(
        Guid tenantId, long additionalBytes, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct)
            ?? throw new InvalidOperationException("Tenant not found.");

        var usage = MapUsage(tenant);
        if (tenant.StorageQuotaBypass)
            return new StorageUploadCheckResult(true, null, usage with { UploadsBlocked = false });

        var projected = tenant.StorageUsedBytes + Math.Max(0, additionalBytes);
        var quota = ResolveQuota(tenant);

        if (projected > quota)
        {
            var msg =
                $"Storage limit reached ({FormatBytes(tenant.StorageUsedBytes)} of {FormatBytes(quota)}). " +
                "Delete old videos/notes or contact your platform provider to upgrade.";
            return new StorageUploadCheckResult(false, msg, usage with { WarningLevel = StorageWarningLevel.Blocked, UploadsBlocked = true });
        }

        var projectedUsage = MapUsage(tenant, projected);
        return new StorageUploadCheckResult(true, null, projectedUsage);
    }

    public async Task RecordUploadAsync(
        Guid tenantId, string storageKey, long sizeBytes, string folder, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct)
            ?? throw new InvalidOperationException("Tenant not found.");

        _db.TenantStorageObjects.Add(new TenantStorageObject
        {
            TenantId = tenantId,
            StorageKey = storageKey,
            Folder = folder,
            SizeBytes = sizeBytes
        });

        tenant.StorageUsedBytes += sizeBytes;
        await _db.SaveChangesAsync(ct);
    }

    public async Task ReleaseAsync(Guid tenantId, string storageKey, CancellationToken ct = default)
    {
        var row = await _db.TenantStorageObjects
            .FirstOrDefaultAsync(o => o.TenantId == tenantId && o.StorageKey == storageKey, ct);
        if (row is null) return;

        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        if (tenant is not null)
            tenant.StorageUsedBytes = Math.Max(0, tenant.StorageUsedBytes - row.SizeBytes);

        _db.TenantStorageObjects.Remove(row);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<TenantStorageUsageDto> SetSuperAdminOverridesAsync(
        Guid tenantId, long? quotaBytesOverride, bool quotaBypass, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct)
            ?? throw new InvalidOperationException("Tenant not found.");

        tenant.StorageQuotaBytesOverride = quotaBytesOverride;
        tenant.StorageQuotaBypass = quotaBypass;
        await _db.SaveChangesAsync(ct);
        return MapUsage(tenant);
    }

    private long ResolveQuota(Tenant tenant) =>
        _options.ResolveQuotaBytes(tenant.Plan, tenant.StorageQuotaBytesOverride);

    private TenantStorageUsageDto MapUsage(Tenant tenant, long? usedOverride = null)
    {
        var used = usedOverride ?? tenant.StorageUsedBytes;
        var quota = ResolveQuota(tenant);
        var percent = quota <= 0 ? 0 : (int)Math.Min(100, Math.Round(100.0 * used / quota));
        var level = percent >= 100
            ? StorageWarningLevel.Full
            : percent >= WarningPercent
                ? StorageWarningLevel.Warning
                : StorageWarningLevel.Ok;
        var blocked = !tenant.StorageQuotaBypass && used >= quota;
        if (blocked) level = StorageWarningLevel.Blocked;

        return new TenantStorageUsageDto(
            tenant.Id,
            tenant.Name,
            tenant.Plan,
            used,
            quota,
            percent,
            level,
            tenant.StorageQuotaBypass,
            blocked);
    }

    internal static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        double kb = bytes / 1024.0;
        if (kb < 1024) return $"{kb:0.#} KB";
        double mb = kb / 1024.0;
        if (mb < 1024) return $"{mb:0.#} MB";
        double gb = mb / 1024.0;
        return $"{gb:0.##} GB";
    }
}
