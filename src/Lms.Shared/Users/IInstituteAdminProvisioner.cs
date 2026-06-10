using Lms.Shared.Common;

namespace Lms.Shared.Users;

public sealed record CreatedInstituteAdminDto(
    Guid UserId,
    string FullName,
    string Email,
    string TempPassword,
    Guid TenantId);

/// <summary>Cross-module contract for SuperAdmin provisioning the first (or additional)
/// InstituteAdmin on a tenant. Implemented by Identity.</summary>
public sealed record ResetInstituteAdminPasswordDto(
    Guid UserId,
    string FullName,
    string Email,
    string TempPassword);

public interface IInstituteAdminProvisioner
{
    Task<Result<CreatedInstituteAdminDto>> CreateAsync(
        Guid tenantId, string email, string fullName, CancellationToken ct = default);

    Task<Result<ResetInstituteAdminPasswordDto>> ResetPasswordAsync(
        Guid tenantId, Guid userId, CancellationToken ct = default);
}
