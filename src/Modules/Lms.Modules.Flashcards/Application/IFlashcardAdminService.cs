namespace Lms.Modules.Flashcards.Application;

public interface IFlashcardAdminService
{
    Task<FlashcardDeckDto?> GetAdminDeckAsync(Guid topicId, CancellationToken ct = default);
    Task<FlashcardDto> AddCardAsync(Guid topicId, CreateFlashcardRequest req, CancellationToken ct = default);
    Task<bool> DeleteCardAsync(Guid cardId, CancellationToken ct = default);
    Task<FlashcardDto?> UpdateCardAsync(Guid cardId, UpdateFlashcardRequest req, CancellationToken ct = default);
    Task<bool> UpdateDeckTitleAsync(Guid topicId, UpdateDeckTitleRequest req, CancellationToken ct = default);
    Task<bool> ReorderCardsAsync(Guid topicId, ReorderCardsRequest req, CancellationToken ct = default);
}
