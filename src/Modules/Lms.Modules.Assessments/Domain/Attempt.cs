using Lms.Shared.Entities;

namespace Lms.Modules.Assessments.Domain;

/// <summary>A student's submission of a quiz. AnswersJson stores the chosen option per question.</summary>
public sealed class Attempt : TenantEntity
{
    public Guid QuizId { get; set; }
    public Guid UserId { get; set; }

    public int Score { get; set; }
    public int Total { get; set; }

    /// <summary>JSON map of questionId -> selectedKey.</summary>
    public string AnswersJson { get; set; } = "{}";

    public DateTime? StartedAt { get; set; }

    /// <summary>When the attempt timer ends (UTC). Null if no time limit.</summary>
    public DateTime? ExpiresAtUtc { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public bool IsInProgress => SubmittedAt is null;
}
