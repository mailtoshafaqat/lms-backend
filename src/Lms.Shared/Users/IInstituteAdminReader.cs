namespace Lms.Shared.Users;

public sealed record InstituteAdminListItemDto(
    Guid UserId,
    string FullName,
    string Email,
    bool IsActive,
    DateTime CreatedAt);

/// <summary>Lists institute admin accounts provisioned on a tenant.</summary>
public interface IInstituteAdminReader
{
    Task<IReadOnlyList<InstituteAdminListItemDto>> ListByTenantAsync(
        Guid tenantId, CancellationToken ct = default);
}
