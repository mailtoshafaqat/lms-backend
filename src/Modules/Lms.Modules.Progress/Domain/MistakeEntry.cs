using Lms.Shared.Entities;

namespace Lms.Modules.Progress.Domain;

/// <summary>Tracks a wrong MCQ answer for the student's mistake diary.</summary>
public sealed class MistakeEntry : TenantEntity
{
    public Guid UserId { get; set; }
    public Guid QuestionId { get; set; }
    public Guid TopicId { get; set; }
    public Guid QuizId { get; set; }
    public string QuizTitle { get; set; } = string.Empty;
    public string Stem { get; set; } = string.Empty;
    public string OptionsJson { get; set; } = "[]";
    public string CorrectKey { get; set; } = string.Empty;
    public string? LastSelectedKey { get; set; }
    public string? Explanation { get; set; }
    public int TimesWrong { get; set; } = 1;
    public DateTime FirstSeenAt { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
}
