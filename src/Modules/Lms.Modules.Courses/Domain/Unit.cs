using Lms.Shared.Entities;

namespace Lms.Modules.Courses.Domain;

public sealed class Unit : TenantEntity
{
    /// <summary>Batch-specific unit; null when this is a shared library unit.</summary>
    public Guid? SubjectId { get; set; }
    public Subject? Subject { get; set; }

    /// <summary>When set, unit lives in the tenant subject catalog (shared library).</summary>
    public Guid? SubjectDefinitionId { get; set; }
    public SubjectDefinition? SubjectDefinition { get; set; }

    public string Title { get; set; } = string.Empty;
    public int Order { get; set; }

    public ICollection<Topic> Topics { get; set; } = new List<Topic>();
}
