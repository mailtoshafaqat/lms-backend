namespace Lms.Modules.Progress.Application;

public interface IBookmarkService
{
    Task<IReadOnlyList<BookmarkDto>> ListAsync(Guid userId, CancellationToken ct = default);
    Task<BookmarkDto> CreateAsync(Guid userId, CreateBookmarkRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid userId, Guid bookmarkId, CancellationToken ct = default);
    Task<BookmarkStatusDto> GetStatusAsync(
        Guid userId, string targetType, Guid targetId, CancellationToken ct = default);
}
