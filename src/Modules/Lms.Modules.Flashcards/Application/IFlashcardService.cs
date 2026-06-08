namespace Lms.Modules.Flashcards.Application;

public interface IFlashcardService
{
    Task<FlashcardDeckDto?> GetByTopicAsync(Guid topicId, CancellationToken ct = default);
}
