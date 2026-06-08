namespace Lms.Shared.Entities;

/// <summary>Base for all persisted entities. Carries identity and audit timestamps.</summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
