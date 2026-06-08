using Lms.Shared.Entities;

namespace Lms.Modules.Courses.Domain;

public sealed class Unit : TenantEntity
{
    public Guid SubjectId { get; set; }
    public Subject? Subject { get; set; }

    public string Title { get; set; } = string.Empty;
    public int Order { get; set; }

    public ICollection<Topic> Topics { get; set; } = new List<Topic>();
}
