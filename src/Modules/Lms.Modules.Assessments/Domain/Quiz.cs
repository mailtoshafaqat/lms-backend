using Lms.Shared.Entities;

namespace Lms.Modules.Assessments.Domain;

public enum QuizType
{
    DailyPracticeTest = 0,
    TopicQuiz = 1,
    UnitTest = 2,
    PyqTest = 3
}

/// <summary>A set of MCQs attached to a topic (e.g. a Daily Practice Test).</summary>
public sealed class Quiz : TenantEntity
{
    public Guid? TopicId { get; set; }
    public Guid? UnitId { get; set; }
    public string Title { get; set; } = string.Empty;
    public QuizType Type { get; set; } = QuizType.DailyPracticeTest;

    /// <summary>When set, only questions of this difficulty are included (topic DPT or assembled unit tests).</summary>
    public QuestionDifficulty? DifficultyFilter { get; set; }

    /// <summary>Minutes allowed once a student starts. Null = no per-attempt time limit.</summary>
    public int? TimeLimitMinutes { get; set; }

    /// <summary>UTC window start. Null = available immediately.</summary>
    public DateTime? AvailableFromUtc { get; set; }

    /// <summary>UTC window end. Null = no end date.</summary>
    public DateTime? AvailableUntilUtc { get; set; }

    public ResultVisibilityMode ResultVisibility { get; set; } = ResultVisibilityMode.Immediate;

    /// <summary>When true, per-question review and explanations are shown once results are visible.</summary>
    public bool ShowExplanations { get; set; } = true;

    /// <summary>Set when teacher publishes results (manual mode).</summary>
    public DateTime? ResultsPublishedAtUtc { get; set; }

    public bool NotifyTeachersOnBatchComplete { get; set; }

    /// <summary>Percent of enrolled students who must submit before teacher notification (default 80).</summary>
    public int BatchCompleteThresholdPercent { get; set; } = 80;

    public bool BatchNotifySent { get; set; }

    public ICollection<Question> Questions { get; set; } = new List<Question>();

    public bool RequiresScheduledAttempt =>
        TimeLimitMinutes is > 0 || AvailableFromUtc is not null || AvailableUntilUtc is not null;

    public bool IsAssembledQuiz => Type is QuizType.UnitTest or QuizType.PyqTest;

    public bool RequiresStartAttempt => RequiresScheduledAttempt || IsAssembledQuiz;
}
