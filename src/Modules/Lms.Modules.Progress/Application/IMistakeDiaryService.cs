namespace Lms.Modules.Progress.Application;

public interface IMistakeDiaryService
{
    Task<IReadOnlyList<MistakeDto>> ListAsync(Guid userId, bool includeResolved = false, CancellationToken ct = default);
    Task<bool> ResolveAsync(Guid userId, Guid mistakeId, CancellationToken ct = default);
}
