using Lms.Modules.Flashcards.Domain;
using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Flashcards.Infrastructure;

/// <summary>Seeds a sample deck (4 cards) per topic (dev only). Topics passed in by the host.</summary>
public static class FlashcardSeeder
{
    public static async Task SeedAsync(
        FlashcardsDbContext db,
        IEnumerable<(Guid TopicId, string Title)> topics,
        CancellationToken ct = default)
    {
        if (await db.Decks.IgnoreQueryFilters().AnyAsync(ct)) return;

        var tenantId = TenantContext.DefaultTenantId;

        foreach (var (topicId, title) in topics)
        {
            var deck = new FlashcardDeck
            {
                TenantId = tenantId,
                TopicId = topicId,
                Title = $"{title} — Key Concepts"
            };

            for (var i = 1; i <= 4; i++)
            {
                deck.Cards.Add(new Flashcard
                {
                    TenantId = tenantId,
                    Front = $"{title}: term {i}?",
                    Back = $"Definition of term {i} in {title}.",
                    Order = i
                });
            }

            db.Decks.Add(deck);
        }

        await db.SaveChangesAsync(ct);
    }
}
