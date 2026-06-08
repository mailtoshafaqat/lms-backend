namespace Lms.Modules.Flashcards.Application;

public sealed record CreateFlashcardRequest(string Front, string Back);

public sealed record UpdateFlashcardRequest(string Front, string Back);

public sealed record UpdateDeckTitleRequest(string Title);

public sealed record ReorderCardsRequest(IReadOnlyList<Guid> CardIds);
