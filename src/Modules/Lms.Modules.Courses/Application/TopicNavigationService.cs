using Lms.Shared.Courses;

namespace Lms.Modules.Courses.Application;

public interface ITopicNavigationService
{
    Task<TopicNavigationDto?> GetNavigationAsync(Guid topicId, CancellationToken ct = default);
}

public sealed record TopicNavItemDto(Guid Id, string Title);

public sealed record TopicNavigationDto(
    TopicNavItemDto? Previous,
    TopicNavItemDto? Next);

public sealed class TopicNavigationService : ITopicNavigationService
{
    private readonly ICourseScopeReader _scope;

    public TopicNavigationService(ICourseScopeReader scope) => _scope = scope;

    public async Task<TopicNavigationDto?> GetNavigationAsync(Guid topicId, CancellationToken ct = default)
    {
        var topicScope = await _scope.GetTopicScopeAsync(topicId, ct);
        if (topicScope is null) return null;

        var ordered = await _scope.GetOrderedTopicsForSubjectAsync(topicScope.SubjectId, ct);
        if (ordered.Count == 0) return new TopicNavigationDto(null, null);

        var index = -1;
        for (var i = 0; i < ordered.Count; i++)
        {
            if (ordered[i].TopicId == topicId)
            {
                index = i;
                break;
            }
        }

        if (index < 0) return new TopicNavigationDto(null, null);

        TopicNavItemDto? prev = index > 0
            ? new TopicNavItemDto(ordered[index - 1].TopicId, ordered[index - 1].TopicTitle)
            : null;

        TopicNavItemDto? next = index < ordered.Count - 1
            ? new TopicNavItemDto(ordered[index + 1].TopicId, ordered[index + 1].TopicTitle)
            : null;

        return new TopicNavigationDto(prev, next);
    }
}
