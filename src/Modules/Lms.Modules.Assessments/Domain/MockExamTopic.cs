using Lms.Shared.Entities;

namespace Lms.Modules.Assessments.Domain;

/// <summary>Topic included in a mock exam. QuestionCount 0 means all questions from the topic quiz.</summary>
public sealed class MockExamTopic : TenantEntity
{
    public Guid MockExamId { get; set; }
    public MockExam? MockExam { get; set; }

    public Guid TopicId { get; set; }
    public string TopicTitle { get; set; } = string.Empty;

    /// <summary>Max questions to pull from this topic. 0 = all available.</summary>
    public int QuestionCount { get; set; }
    public int Order { get; set; }
}
