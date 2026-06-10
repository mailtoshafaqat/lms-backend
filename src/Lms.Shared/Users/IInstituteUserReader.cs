namespace Lms.Shared.Users;

public sealed record TeacherContactDto(Guid UserId, string Email, string FullName);

/// <summary>Read-only user lookups for cross-module validation (Courses, LiveClasses, …).</summary>
public interface IInstituteUserReader
{
    Task<bool> IsActiveTeacherAsync(Guid userId, CancellationToken ct = default);

    Task<IReadOnlyDictionary<Guid, string>> GetTeacherDisplayNamesAsync(
        IEnumerable<Guid> userIds, CancellationToken ct = default);

    Task<IReadOnlyList<TeacherContactDto>> GetTeacherContactsAsync(
        IEnumerable<Guid> userIds, CancellationToken ct = default);
}
