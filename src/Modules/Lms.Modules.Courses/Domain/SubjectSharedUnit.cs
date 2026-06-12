using Lms.Shared.Entities;

namespace Lms.Modules.Courses.Domain;

/// <summary>Links a shared library unit into a batch subject's content tree.</summary>
public sealed class SubjectSharedUnit : TenantEntity
{
    public Guid SubjectId { get; set; }
    public Subject? Subject { get; set; }
    public Guid UnitId { get; set; }
    public Unit? Unit { get; set; }
    public int Order { get; set; }
}
