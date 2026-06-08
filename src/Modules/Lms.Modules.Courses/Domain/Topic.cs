using Lms.Shared.Entities;

namespace Lms.Modules.Courses.Domain;

/// <summary>Leaf of the content tree. Holds counts for video/MCQs/flashcards (the actual
/// media and questions are owned by their own modules in later phases).</summary>
public sealed class Topic : TenantEntity
{
    public Guid UnitId { get; set; }
    public Unit? Unit { get; set; }

    public string Title { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool HasVideo { get; set; }
    public int McqCount { get; set; }
    public int FlashcardCount { get; set; }
}
