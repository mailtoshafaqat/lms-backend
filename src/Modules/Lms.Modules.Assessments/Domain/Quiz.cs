using Lms.Shared.Entities;

namespace Lms.Modules.Assessments.Domain;

public enum QuizType
{
    DailyPracticeTest = 0,
    TopicQuiz = 1
}

/// <summary>A set of MCQs attached to a topic (e.g. a Daily Practice Test).</summary>
public sealed class Quiz : TenantEntity
{
    public Guid TopicId { get; set; }
    public string Title { get; set; } = string.Empty;
    public QuizType Type { get; set; } = QuizType.DailyPracticeTest;

    public ICollection<Question> Questions { get; set; } = new List<Question>();
}
