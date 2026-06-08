using Lms.Modules.Flashcards.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Flashcards.Application;

public sealed class FlashcardService : IFlashcardService
{
    private readonly FlashcardsDbContext _db;

    public FlashcardService(FlashcardsDbContext db) => _db = db;

    public async Task<FlashcardDeckDto?> GetByTopicAsync(Guid topicId, CancellationToken ct = default)
    {
        var deck = await _db.Decks
            .Include(d => d.Cards)
            .FirstOrDefaultAsync(d => d.TopicId == topicId, ct);

        if (deck is null) return null;

        return new FlashcardDeckDto(
            deck.Id,
            deck.TopicId,
            deck.Title,
            deck.Cards
                .OrderBy(c => c.Order)
                .Select(c => new FlashcardDto(c.Id, c.Front, c.Back, c.Order))
                .ToList());
    }
}
