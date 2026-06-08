using Lms.Shared.Entities;

namespace Lms.Modules.Content.Domain;

/// <summary>Study notes for a topic. Either inline HTML or a downloadable file (storage key).</summary>
public sealed class Note : TenantEntity
{
    public Guid TopicId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ContentHtml { get; set; }
    public string? StorageKey { get; set; }
    public int Order { get; set; }
}
