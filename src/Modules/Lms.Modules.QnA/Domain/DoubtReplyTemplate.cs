using Lms.Shared.Entities;

namespace Lms.Modules.QnA.Domain;

/// <summary>Reusable canned reply for teachers answering student doubts.</summary>
public sealed class DoubtReplyTemplate : TenantEntity
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public int Order { get; set; }
}
