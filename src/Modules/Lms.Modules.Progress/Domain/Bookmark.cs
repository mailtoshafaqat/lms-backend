using Lms.Shared.Entities;

namespace Lms.Modules.Progress.Domain;

public static class BookmarkTargetTypes
{
    public const string Topic = "Topic";
    public const string Question = "Question";
}

/// <summary>Student-saved topic or MCQ for quick revision.</summary>
public sealed class Bookmark : TenantEntity
{
    public Guid UserId { get; set; }
    public string TargetType { get; set; } = BookmarkTargetTypes.Topic;
    public Guid TargetId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public Guid? TopicId { get; set; }
}
