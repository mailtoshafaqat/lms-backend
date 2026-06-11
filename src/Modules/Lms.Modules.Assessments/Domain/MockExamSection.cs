using Lms.Shared.Entities;

namespace Lms.Modules.Assessments.Domain;

/// <summary>Blueprint section (e.g. Biology, Chemistry) within a mock exam.</summary>
public sealed class MockExamSection : TenantEntity
{
    public Guid MockExamId { get; set; }
    public MockExam? MockExam { get; set; }

    public string Title { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    /// <summary>Optional per-section time cap in minutes.</summary>
    public int? SectionTimeLimitMinutes { get; set; }

    public ICollection<MockExamTopic> Topics { get; set; } = new List<MockExamTopic>();
}
