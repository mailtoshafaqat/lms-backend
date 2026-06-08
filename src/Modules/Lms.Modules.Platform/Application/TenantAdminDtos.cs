using Lms.Shared.Tenancy;

namespace Lms.Modules.Platform.Application;

public sealed record TenantListItemDto(
    Guid Id,
    string Name,
    string Slug,
    TenantStatus Status,
    string Plan,
    DateTime CreatedAt);

public sealed record TenantDetailDto(
    Guid Id,
    string Name,
    string Slug,
    string? CustomDomain,
    TenantStatus Status,
    string Plan,
    bool LiveClassesEnabled,
    ZoomMode ZoomMode,
    PaymentMode PaymentMode,
    bool AllowStudentSelfEnroll,
    bool AllowAdminCreateStudent,
    DateTime CreatedAt);

public sealed record CreateTenantRequest(
    string Name,
    string Slug,
    string Plan);

public sealed record UpdateTenantFlagsRequest(
    TenantStatus Status,
    string Plan,
    string? CustomDomain,
    bool LiveClassesEnabled,
    ZoomMode ZoomMode,
    PaymentMode PaymentMode,
    bool AllowStudentSelfEnroll,
    bool AllowAdminCreateStudent);

public sealed record CreateTenantAdminRequest(string FullName, string Email);
