using Lms.Shared.Entities;

namespace Lms.Modules.Identity.Domain;

/// <summary>Parent or guardian contact for progress report emails.</summary>
public sealed class StudentGuardian : TenantEntity
{
    public Guid StudentUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>When true, a weekly progress report may be sent (hosted job stub).</summary>
    public bool WeeklyReportsEnabled { get; set; }
}
