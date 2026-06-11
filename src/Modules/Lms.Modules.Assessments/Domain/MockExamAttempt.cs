using Lms.Shared.Entities;

namespace Lms.Modules.Assessments.Domain;

/// <summary>Student attempt on a mock exam. QuestionIdsJson stores the assembled question ids.</summary>
public sealed class MockExamAttempt : TenantEntity
{
    public Guid MockExamId { get; set; }
    public Guid UserId { get; set; }

    public decimal Score { get; set; }
    public int Total { get; set; }
    public int CorrectCount { get; set; }
    public int WrongCount { get; set; }

    /// <summary>JSON array of question GUIDs included in this attempt.</summary>
    public string QuestionIdsJson { get; set; } = "[]";

    /// <summary>JSON map of questionId -> selectedKey.</summary>
    public string AnswersJson { get; set; } = "{}";

    /// <summary>JSON array of question ids flagged for review.</summary>
    public string FlaggedQuestionIdsJson { get; set; } = "[]";

    public DateTime? StartedAt { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime? SubmittedAt { get; set; }

    public bool IsInProgress => SubmittedAt is null;
}
