namespace Lms.Shared.Common;

/// <summary>Standard query parameters for paginated admin list endpoints.</summary>
public class PagedListQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public string? Search { get; set; }
    public string? SortBy { get; set; }
    public string? SortDir { get; set; }

    /// <summary>Filter students enrolled in bundles containing a batch subject linked to this catalog entry.</summary>
    public Guid? SubjectDefinitionId { get; set; }

    public int NormalizedPage => Page < 1 ? 1 : Page;

    public int NormalizedPageSize => PageSize switch
    {
        > 100 => 100,
        < 1 => 25,
        _ => PageSize
    };

    public bool IsDescending =>
        string.Equals(SortDir, "desc", StringComparison.OrdinalIgnoreCase);

    public string? NormalizedSearch =>
        string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();
}
