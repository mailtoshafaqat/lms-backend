using Lms.Modules.Progress.Domain;
using Lms.Modules.Progress.Infrastructure;
using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Progress.Application;

public sealed class BookmarkService : IBookmarkService
{
    private readonly ProgressDbContext _db;
    private readonly ITenantContext _tenant;

    public BookmarkService(ProgressDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<BookmarkDto>> ListAsync(Guid userId, CancellationToken ct = default) =>
        await _db.Bookmarks.AsNoTracking()
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new BookmarkDto(
                b.Id,
                b.TargetType,
                b.TargetId,
                b.Title,
                b.Subtitle,
                b.TopicId,
                b.CreatedAt))
            .ToListAsync(ct);

    public async Task<BookmarkDto> CreateAsync(
        Guid userId, CreateBookmarkRequest request, CancellationToken ct = default)
    {
        var targetType = NormalizeTargetType(request.TargetType);
        var existing = await _db.Bookmarks
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                b => b.UserId == userId &&
                     b.TargetType == targetType &&
                     b.TargetId == request.TargetId,
                ct);
        if (existing is not null)
        {
            if (existing.TenantId != _tenant.TenantId)
            {
                existing.TenantId = _tenant.TenantId;
                await _db.SaveChangesAsync(ct);
            }

            return Map(existing);
        }

        var title = request.Title.Trim();
        if (string.IsNullOrEmpty(title))
            throw new InvalidOperationException("Bookmark title is required.");

        var bookmark = new Bookmark
        {
            TenantId = _tenant.TenantId,
            UserId = userId,
            TargetType = targetType,
            TargetId = request.TargetId,
            Title = title.Length > 300 ? title[..300] : title,
            Subtitle = string.IsNullOrWhiteSpace(request.Subtitle) ? null : request.Subtitle.Trim(),
            TopicId = request.TopicId
        };
        _db.Bookmarks.Add(bookmark);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            var again = await _db.Bookmarks
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(
                    b => b.UserId == userId &&
                         b.TargetType == targetType &&
                         b.TargetId == request.TargetId,
                    ct);
            if (again is not null) return Map(again);
            throw;
        }

        return Map(bookmark);
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid bookmarkId, CancellationToken ct = default)
    {
        var row = await _db.Bookmarks
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(b => b.Id == bookmarkId && b.UserId == userId, ct);
        if (row is null) return false;
        _db.Bookmarks.Remove(row);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<BookmarkStatusDto> GetStatusAsync(
        Guid userId, string targetType, Guid targetId, CancellationToken ct = default)
    {
        var normalized = NormalizeTargetType(targetType);
        var row = await _db.Bookmarks.AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                b => b.UserId == userId && b.TargetType == normalized && b.TargetId == targetId,
                ct);
        return new BookmarkStatusDto(row is not null, row?.Id);
    }

    private static string NormalizeTargetType(string targetType) =>
        targetType.Equals(BookmarkTargetTypes.Question, StringComparison.OrdinalIgnoreCase)
            ? BookmarkTargetTypes.Question
            : BookmarkTargetTypes.Topic;

    private static BookmarkDto Map(Bookmark b) => new(
        b.Id,
        b.TargetType,
        b.TargetId,
        b.Title,
        b.Subtitle,
        b.TopicId,
        b.CreatedAt);
}
