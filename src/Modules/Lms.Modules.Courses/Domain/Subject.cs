using Lms.Shared.Entities;

namespace Lms.Modules.Courses.Domain;

public sealed class Subject : TenantEntity
{
    public Guid BundleId { get; set; }
    public Bundle? Bundle { get; set; }

    public string Title { get; set; } = string.Empty;
    public int Order { get; set; }

    public ICollection<Unit> Units { get; set; } = new List<Unit>();
}
