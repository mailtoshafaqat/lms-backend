using Lms.Shared.Tenancy;

namespace Lms.Modules.Platform.Application;

public sealed record TenantListItemDto(
    Guid Id,
    string Name,
    string Slug,
    TenantStatus Status,
    string Plan,
    DateTime? TrialEndsAt,
    DateTime CreatedAt,
    long StorageUsedBytes,
    long StorageQuotaBytes,
    int StorageUsedPercent,
    bool StorageQuotaBypass);

public sealed record UpdateTenantStorageRequest(
    long? QuotaBytesOverride,
    bool QuotaBypass);

public sealed record TenantDetailDto(
    Guid Id,
    string Name,
    string Slug,
    string? CustomDomain,
    TenantStatus Status,
    string Plan,
    ProductProfile ProductProfile,
    bool LiveClassesEnabled,
    ZoomMode ZoomMode,
    PaymentMode PaymentMode,
    bool AllowStudentSelfEnroll,
    bool AllowAdminCreateStudent,
    bool SyllabusMentorEnabled,
    bool BundlePriceEditEnabled,
    bool McqBulkImportEnabled,
    DateTime? TrialEndsAt,
    DateTime CreatedAt);

public sealed record CreateTenantRequest(
    string Name,
    string Slug,
    string Plan,
    ProductProfile ProductProfile = ProductProfile.ExamPrep);

public sealed record UpdateTenantFlagsRequest(
    TenantStatus Status,
    string Plan,
    ProductProfile ProductProfile,
    string? CustomDomain,
    bool LiveClassesEnabled,
    ZoomMode ZoomMode,
    PaymentMode PaymentMode,
    bool AllowStudentSelfEnroll,
    bool AllowAdminCreateStudent,
    bool SyllabusMentorEnabled,
    bool BundlePriceEditEnabled,
    bool McqBulkImportEnabled,
    DateTime? TrialEndsAt = null);

public sealed record CreateTenantAdminRequest(string FullName, string Email);
