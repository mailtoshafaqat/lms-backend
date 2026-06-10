using Lms.Modules.Identity.Infrastructure;
using Lms.Shared.Auth;
using Lms.Shared.Users;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Identity.Infrastructure;

public sealed class InstituteUserReader : IInstituteUserReader
{
    private readonly IdentityDbContext _db;

    public InstituteUserReader(IdentityDbContext db) => _db = db;

    public Task<bool> IsActiveTeacherAsync(Guid userId, CancellationToken ct = default) =>
        _db.Users.AnyAsync(
            u => u.Id == userId && u.Role == Roles.Teacher && u.IsActive, ct);

    public async Task<IReadOnlyDictionary<Guid, string>> GetTeacherDisplayNamesAsync(
        IEnumerable<Guid> userIds, CancellationToken ct = default)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<Guid, string>();

        return await _db.Users
            .Where(u => ids.Contains(u.Id) && u.Role == Roles.Teacher)
            .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);
    }

    public async Task<IReadOnlyList<TeacherContactDto>> GetTeacherContactsAsync(
        IEnumerable<Guid> userIds, CancellationToken ct = default)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0) return [];

        return await _db.Users.AsNoTracking()
            .Where(u => ids.Contains(u.Id) && u.Role == Roles.Teacher && u.IsActive)
            .Select(u => new TeacherContactDto(u.Id, u.Email, u.FullName))
            .ToListAsync(ct);
    }
}
