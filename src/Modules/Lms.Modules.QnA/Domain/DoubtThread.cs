using Lms.Shared.Entities;

namespace Lms.Modules.QnA.Domain;

public sealed class DoubtThread : TenantEntity
{
    public Guid SubjectId { get; set; }
    public string SubjectTitle { get; set; } = string.Empty;
    public Guid BundleId { get; set; }
    public string BundleTitle { get; set; } = string.Empty;
    public Guid StudentUserId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public Guid? TopicId { get; set; }
    public string? TopicTitle { get; set; }
    public string Title { get; set; } = string.Empty;
    public DoubtThreadStatus Status { get; set; } = DoubtThreadStatus.Open;
    public DateTime? ResolvedAt { get; set; }
    public Guid? ResolvedByUserId { get; set; }

    public ICollection<DoubtMessage> Messages { get; set; } = [];
}
