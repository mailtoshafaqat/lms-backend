using Lms.Shared.Users;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Identity.Infrastructure;

/// <summary>Identity's implementation of the shared <see cref="IUserDirectory"/> contract.</summary>
public sealed class UserDirectory : IUserDirectory
{
    private readonly IdentityDbContext _db;

    public UserDirectory(IdentityDbContext db) => _db = db;

    public async Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(
        IEnumerable<Guid> userIds, CancellationToken ct = default)
    {
        var contacts = await GetContactsAsync(userIds, ct);
        return contacts.ToDictionary(kv => kv.Key, kv => kv.Value.FullName);
    }

    public async Task<IReadOnlyDictionary<Guid, UserContactSummary>> GetContactsAsync(
        IEnumerable<Guid> userIds, CancellationToken ct = default)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<Guid, UserContactSummary>();

        return await _db.Users
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => new UserContactSummary(u.FullName, u.Email), ct);
    }
}
