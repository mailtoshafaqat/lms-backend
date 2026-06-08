using Lms.Shared.Entities;

namespace Lms.Modules.Platform.Domain;

/// <summary>Per-tenant public landing page (ordered sections stored as data).</summary>
public sealed class LandingPage : TenantEntity
{
    public ICollection<PageSection> Sections { get; set; } = [];
}
