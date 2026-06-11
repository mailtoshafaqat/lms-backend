using Lms.Shared.Entities;

namespace Lms.Modules.Assessments.Domain;

/// <summary>Multi-topic timed mock exam assembled from topic quizzes.</summary>
public sealed class MockExam : TenantEntity
{
    public Guid BundleId { get; set; }
    public string BundleTitle { get; set; } = string.Empty;
    public Guid SubjectId { get; set; }
    public string SubjectTitle { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public int TimeLimitMinutes { get; set; }

    /// <summary>Marks awarded per correct answer (e.g. 4 for MDCAT).</summary>
    public decimal MarksPerCorrect { get; set; } = 1m;

    /// <summary>Marks deducted per wrong answer (e.g. 1 for MDCAT negative marking).</summary>
    public decimal PenaltyPerWrong { get; set; }

    public ICollection<MockExamSection> Sections { get; set; } = new List<MockExamSection>();
    public DateTime? AvailableFromUtc { get; set; }
    public DateTime? AvailableUntilUtc { get; set; }
    public bool IsPublished { get; set; }
    public bool IsArchived { get; set; }

    public ResultVisibilityMode ResultVisibility { get; set; } = ResultVisibilityMode.AfterClose;

    public bool ShowExplanations { get; set; } = true;

    public DateTime? ResultsPublishedAtUtc { get; set; }

    public bool NotifyTeachersOnBatchComplete { get; set; } = true;

    public int BatchCompleteThresholdPercent { get; set; } = 80;

    public bool BatchNotifySent { get; set; }

    public ICollection<MockExamTopic> Topics { get; set; } = new List<MockExamTopic>();
}
