namespace Lms.Modules.Content.Application;

public interface IContentService
{
    Task<TopicContentDto> GetTopicContentAsync(Guid topicId, CancellationToken ct = default);
}
