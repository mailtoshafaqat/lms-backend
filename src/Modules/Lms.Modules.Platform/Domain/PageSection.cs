using Lms.Shared.Entities;

namespace Lms.Modules.Platform.Domain;

/// <summary>Typed, reorderable block on a tenant landing page. Content is JSON per section type.</summary>
public sealed class PageSection : BaseEntity
{
    public Guid LandingPageId { get; set; }
    public LandingPage? LandingPage { get; set; }

    /// <summary>Hero | Features | Footer</summary>
    public string SectionType { get; set; } = string.Empty;

    public int SortOrder { get; set; }
    public string ContentJson { get; set; } = "{}";
    public bool IsEnabled { get; set; } = true;
}
