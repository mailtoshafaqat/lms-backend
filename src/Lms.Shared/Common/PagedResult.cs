namespace Lms.Shared.Common;

public sealed record PagedResult<T>(IReadOnlyList<T> Data, int Page, int PageSize, int Total);
