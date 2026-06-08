using Lms.Shared.Entities;

namespace Lms.Modules.Flashcards.Domain;

/// <summary>A set of flashcards attached to a topic.</summary>
public sealed class FlashcardDeck : TenantEntity
{
    public Guid TopicId { get; set; }
    public string Title { get; set; } = string.Empty;

    public ICollection<Flashcard> Cards { get; set; } = new List<Flashcard>();
}
