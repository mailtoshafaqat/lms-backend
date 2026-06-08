using Lms.Shared.Common;
using Lms.Shared.Users;

namespace Lms.Modules.Platform.Application;

public interface ITenantAdminService
{
    Task<IReadOnlyList<TenantListItemDto>> ListAsync(CancellationToken ct = default);
    Task<TenantDetailDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<Result<TenantDetailDto>> CreateAsync(CreateTenantRequest request, CancellationToken ct = default);
    Task<Result<TenantDetailDto>> UpdateFlagsAsync(Guid id, UpdateTenantFlagsRequest request, CancellationToken ct = default);
    Task<Result<CreatedInstituteAdminDto>> CreateInstituteAdminAsync(
        Guid tenantId, CreateTenantAdminRequest request, CancellationToken ct = default);
}
