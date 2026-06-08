using Lms.Modules.Flashcards.Domain;
using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Flashcards.Infrastructure;

public sealed class FlashcardsDbContext : DbContext
{
    private readonly ITenantContext _tenant;

    public FlashcardsDbContext(DbContextOptions<FlashcardsDbContext> options, ITenantContext tenant)
        : base(options)
    {
        _tenant = tenant;
    }

    public DbSet<FlashcardDeck> Decks => Set<FlashcardDeck>();
    public DbSet<Flashcard> Cards => Set<Flashcard>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("flashcards");

        builder.Entity<FlashcardDeck>(e =>
        {
            e.ToTable("Decks");
            e.Property(d => d.Title).IsRequired().HasMaxLength(200);
            e.HasIndex(d => d.TopicId);
            e.HasQueryFilter(d => d.TenantId == _tenant.TenantId);
            e.HasMany(d => d.Cards).WithOne(c => c.Deck!).HasForeignKey(c => c.DeckId);
        });

        builder.Entity<Flashcard>(e =>
        {
            e.ToTable("Cards");
            e.Property(c => c.Front).IsRequired();
            e.Property(c => c.Back).IsRequired();
            e.HasQueryFilter(c => c.TenantId == _tenant.TenantId);
        });

        base.OnModelCreating(builder);
    }
}
