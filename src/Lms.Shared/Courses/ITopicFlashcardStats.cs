namespace Lms.Shared.Courses;

/// <summary>Updates denormalized flashcard counts on course topics.</summary>
public interface ITopicFlashcardStats
{
    Task SetFlashcardCountAsync(Guid topicId, int count, CancellationToken ct = default);
}
