using Lms.Shared.Entities;

namespace Lms.Modules.Flashcards.Domain;

public sealed class Flashcard : TenantEntity
{
    public Guid DeckId { get; set; }
    public FlashcardDeck? Deck { get; set; }

    public string Front { get; set; } = string.Empty;
    public string Back { get; set; } = string.Empty;
    public int Order { get; set; }
}
