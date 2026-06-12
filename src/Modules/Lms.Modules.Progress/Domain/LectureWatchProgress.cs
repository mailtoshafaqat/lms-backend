using Lms.Shared.Entities;

namespace Lms.Modules.Progress.Domain;

/// <summary>Per-user watch position for a lecture. ProgressPercent is monotonic (0–100).</summary>
public sealed class LectureWatchProgress : TenantEntity
{
    public Guid UserId { get; set; }
    public Guid LectureId { get; set; }
    public Guid TopicId { get; set; }
    public int ProgressPercent { get; set; }
    public int PositionSec { get; set; }
    public DateTime LastWatchedAt { get; set; } = DateTime.UtcNow;
}
