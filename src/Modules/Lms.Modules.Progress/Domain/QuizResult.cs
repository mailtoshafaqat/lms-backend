using Lms.Shared.Entities;

namespace Lms.Modules.Progress.Domain;

/// <summary>A recorded quiz outcome for a user. Written by the QuizSubmitted event handler;
/// read by My Grades and the leaderboard. Denormalized (QuizTitle/TopicId) so Progress
/// does not query Courses/Assessments on every read.</summary>
public sealed class QuizResult : TenantEntity
{
    public Guid UserId { get; set; }
    public Guid QuizId { get; set; }
    public Guid TopicId { get; set; }
    public string QuizTitle { get; set; } = string.Empty;
    public int Score { get; set; }
    public int Total { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public int Percentage => Total == 0 ? 0 : (int)Math.Round(100.0 * Score / Total);
}
