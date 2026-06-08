namespace Lms.Shared.Users;

/// <summary>Cross-module, well-defined interface for resolving user display names.
/// Owned by the shared kernel, implemented by the Identity module, consumed by any module
/// (e.g. Progress leaderboard) that needs to show who a user is — without depending on Identity internals.</summary>
public interface IUserDirectory
{
    Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(
        IEnumerable<Guid> userIds, CancellationToken ct = default);
}
