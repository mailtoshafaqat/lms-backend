using Lms.Shared.Entities;

namespace Lms.Modules.Courses.Domain;

/// <summary>Assigns a teacher user to a subject within a bundle (many-to-many).</summary>
public sealed class SubjectTeacher : TenantEntity
{
    public Guid SubjectId { get; set; }
    public Subject? Subject { get; set; }
    public Guid UserId { get; set; }
}
