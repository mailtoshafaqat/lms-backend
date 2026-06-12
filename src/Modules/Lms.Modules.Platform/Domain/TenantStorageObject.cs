namespace Lms.Modules.Platform.Domain;

/// <summary>Tracks a blob stored for a tenant (for quota metering on shared VPS / object storage).</summary>
public sealed class TenantStorageObject
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public string Folder { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
