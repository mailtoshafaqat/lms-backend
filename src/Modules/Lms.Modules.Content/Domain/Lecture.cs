using Lms.Shared.Entities;

namespace Lms.Modules.Content.Domain;

/// <summary>A video lecture attached to a topic. Source is either an external URL
/// (CDN/Zoom recording) or a storage key served via IFileStorage.</summary>
public sealed class Lecture : TenantEntity
{
    public Guid TopicId { get; set; }
    public string Title { get; set; } = string.Empty;

    /// <summary>External playback URL (CDN/HLS). Null when the file is stored locally.</summary>
    public string? Url { get; set; }

    /// <summary>IFileStorage key when the video is stored by us. Null when using an external URL.</summary>
    public string? StorageKey { get; set; }

    public int DurationSec { get; set; }
    public int Order { get; set; }

    /// <summary>When true, playback URL is only returned to authenticated users.</summary>
    public bool MembersOnly { get; set; }

    /// <summary>Links a Zoom recording lecture back to its live class.</summary>
    public Guid? SourceLiveClassId { get; set; }
}
