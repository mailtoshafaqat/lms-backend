using Lms.Shared.Entities;

namespace Lms.Modules.Assessments.Domain;

/// <summary>A single MCQ. Options are stored as JSON; CorrectKey is the 0-based index
/// (as string) of the right option.</summary>
public sealed class Question : TenantEntity
{
    public Guid QuizId { get; set; }
    public Quiz? Quiz { get; set; }

    public string Stem { get; set; } = string.Empty;

    /// <summary>JSON array of option texts, e.g. ["A","B","C","D"].</summary>
    public string OptionsJson { get; set; } = "[]";

    /// <summary>0-based index of the correct option (as string).</summary>
    public string CorrectKey { get; set; } = "0";

    public string? Explanation { get; set; }
    public int Order { get; set; }

    public QuestionDifficulty Difficulty { get; set; } = QuestionDifficulty.Medium;

    public bool IsPyq { get; set; }
    public int? PyqYear { get; set; }
    public string? PyqExam { get; set; }
}
