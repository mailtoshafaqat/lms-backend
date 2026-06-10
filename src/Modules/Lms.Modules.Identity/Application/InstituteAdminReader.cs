using Lms.Modules.Identity.Infrastructure;
using Lms.Shared.Auth;
using Lms.Shared.Users;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Identity.Application;

public sealed class InstituteAdminReader : IInstituteAdminReader
{
    private readonly IdentityDbContext _db;

    public InstituteAdminReader(IdentityDbContext db) => _db = db;

    public async Task<IReadOnlyList<InstituteAdminListItemDto>> ListByTenantAsync(
        Guid tenantId, CancellationToken ct = default) =>
        await _db.Users
            .AsNoTracking()
            .Where(u => u.TenantId == tenantId && u.Role == Roles.InstituteAdmin)
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new InstituteAdminListItemDto(
                u.Id, u.FullName, u.Email, u.IsActive, u.CreatedAt))
            .ToListAsync(ct);
}
