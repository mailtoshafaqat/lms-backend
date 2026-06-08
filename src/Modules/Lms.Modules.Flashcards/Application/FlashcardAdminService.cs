using Lms.Modules.Flashcards.Domain;
using Lms.Modules.Flashcards.Infrastructure;
using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Flashcards.Application;

public sealed class FlashcardAdminService : IFlashcardAdminService
{
    private readonly FlashcardsDbContext _db;
    private readonly ITenantContext _tenant;

    public FlashcardAdminService(FlashcardsDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<FlashcardDeckDto?> GetAdminDeckAsync(Guid topicId, CancellationToken ct = default)
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

    public async Task<FlashcardDto> AddCardAsync(Guid topicId, CreateFlashcardRequest req, CancellationToken ct = default)
    {
        var deck = await _db.Decks
            .Include(d => d.Cards)
            .FirstOrDefaultAsync(d => d.TopicId == topicId, ct);

        if (deck is null)
        {
            deck = new FlashcardDeck
            {
                TenantId = _tenant.TenantId,
                TopicId = topicId,
                Title = "Key Concepts"
            };
            _db.Decks.Add(deck);
        }

        var order = deck.Cards.Count == 0 ? 1 : deck.Cards.Max(c => c.Order) + 1;

        var card = new Flashcard
        {
            TenantId = _tenant.TenantId,
            Front = req.Front.Trim(),
            Back = req.Back.Trim(),
            Order = order
        };
        deck.Cards.Add(card);

        await _db.SaveChangesAsync(ct);
        return new FlashcardDto(card.Id, card.Front, card.Back, card.Order);
    }

    public async Task<bool> DeleteCardAsync(Guid cardId, CancellationToken ct = default)
    {
        var card = await _db.Cards.FindAsync([cardId], ct);
        if (card is null) return false;
        _db.Cards.Remove(card);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<FlashcardDto?> UpdateCardAsync(Guid cardId, UpdateFlashcardRequest req, CancellationToken ct = default)
    {
        var card = await _db.Cards.FindAsync([cardId], ct);
        if (card is null) return null;
        card.Front = req.Front.Trim();
        card.Back = req.Back.Trim();
        await _db.SaveChangesAsync(ct);
        return new FlashcardDto(card.Id, card.Front, card.Back, card.Order);
    }

    public async Task<bool> UpdateDeckTitleAsync(Guid topicId, UpdateDeckTitleRequest req, CancellationToken ct = default)
    {
        var deck = await _db.Decks.FirstOrDefaultAsync(d => d.TopicId == topicId, ct);
        if (deck is null) return false;
        deck.Title = req.Title.Trim();
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ReorderCardsAsync(Guid topicId, ReorderCardsRequest req, CancellationToken ct = default)
    {
        var deck = await _db.Decks.Include(d => d.Cards).FirstOrDefaultAsync(d => d.TopicId == topicId, ct);
        if (deck is null) return false;
        var order = 1;
        foreach (var id in req.CardIds)
        {
            var card = deck.Cards.FirstOrDefault(c => c.Id == id);
            if (card is null) return false;
            card.Order = order++;
        }
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
