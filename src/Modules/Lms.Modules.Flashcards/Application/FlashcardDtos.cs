namespace Lms.Modules.Flashcards.Application;

public sealed record FlashcardDto(Guid Id, string Front, string Back, int Order);

public sealed record FlashcardDeckDto(Guid Id, Guid TopicId, string Title, IReadOnlyList<FlashcardDto> Cards);
