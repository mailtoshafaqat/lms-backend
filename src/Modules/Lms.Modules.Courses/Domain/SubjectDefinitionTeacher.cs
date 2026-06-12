using Lms.Shared.Entities;

namespace Lms.Modules.Courses.Domain;

/// <summary>Assigns a teacher to a catalog subject; grants access to all linked batch placements.</summary>
public sealed class SubjectDefinitionTeacher : TenantEntity
{
    public Guid SubjectDefinitionId { get; set; }
    public SubjectDefinition? SubjectDefinition { get; set; }
    public Guid UserId { get; set; }
}
