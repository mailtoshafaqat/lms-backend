using Lms.Shared.Entities;

namespace Lms.Modules.QnA.Domain;

public sealed class DoubtMessage : TenantEntity
{
    public Guid ThreadId { get; set; }
    public DoubtThread? Thread { get; set; }
    public Guid AuthorUserId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorRole { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}
