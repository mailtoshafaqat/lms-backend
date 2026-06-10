using Microsoft.EntityFrameworkCore;

namespace Lms.Shared.Common;

public static class PagedQueryExtensions
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        PagedListQuery paging,
        CancellationToken ct = default)
    {
        var page = paging.NormalizedPage;
        var pageSize = paging.NormalizedPageSize;
        var total = await query.CountAsync(ct);
        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return new PagedResult<T>(data, page, pageSize, total);
    }

    public static bool ResolveDescending(PagedListQuery query, bool defaultDescending)
    {
        if (string.IsNullOrWhiteSpace(query.SortDir))
            return defaultDescending;
        return query.IsDescending;
    }
}
