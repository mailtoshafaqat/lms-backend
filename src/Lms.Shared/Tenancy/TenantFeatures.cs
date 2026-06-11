namespace Lms.Shared.Tenancy;

public enum TenantStatus
{
    Trial = 0,
    Active = 1,
    Suspended = 2
}

/// <summary>Institute connects and pays for its own Zoom account (BYO).</summary>
public enum ZoomMode
{
    Disabled = 0,
    TenantManaged = 1
}

/// <summary>TenantManaged = institute collects fees; PlatformManaged = LMS checkout (Phase 3).</summary>
public enum PaymentMode
{
    TenantManaged = 0,
    PlatformManaged = 1
}

/// <summary>Per-tenant capability flags set by SuperAdmin. Drives what InstituteAdmin and students can do.</summary>
public sealed record TenantFeatures(
    Guid TenantId,
    string TenantName,
    string Slug,
    TenantStatus Status,
    string Plan,
    ProductProfile ProductProfile,
    bool MockExamsEnabled,
    bool UnitPyqTestsEnabled,
    bool MistakeDiaryEnabled,
    bool DoubtsEnabled,
    bool SyllabusMentorEnabled,
    bool LiveClassesEnabled,
    ZoomMode ZoomMode,
    PaymentMode PaymentMode,
    bool AllowStudentSelfEnroll,
    bool AllowAdminCreateStudent,
    bool BundlePriceEditEnabled,
    bool McqBulkImportEnabled);

public interface ITenantFeaturesProvider
{
    Task<TenantFeatures?> GetAsync(Guid tenantId, CancellationToken ct = default);
}
