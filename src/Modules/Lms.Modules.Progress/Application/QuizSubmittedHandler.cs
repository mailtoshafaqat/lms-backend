using Lms.Modules.Assessments.Contracts;
using Lms.Modules.Progress.Domain;
using Lms.Modules.Progress.Infrastructure;
using Lms.Shared.Courses;
using Lms.Shared.Events;

namespace Lms.Modules.Progress.Application;

/// <summary>Reacts to the Assessments module's QuizSubmitted event and records a grade.
/// Progress does not call Assessments; it just listens. This is the async / event half of
/// the hybrid communication model.</summary>
public sealed class QuizSubmittedHandler : IEventHandler<QuizSubmittedEvent>
{
    private readonly ProgressDbContext _db;
    private readonly ICourseScopeReader _scope;
    private readonly ICertificateService _certificates;

    public QuizSubmittedHandler(
        ProgressDbContext db,
        ICourseScopeReader scope,
        ICertificateService certificates)
    {
        _db = db;
        _scope = scope;
        _certificates = certificates;
    }

    public async Task HandleAsync(QuizSubmittedEvent @event, CancellationToken cancellationToken = default)
    {
        _db.QuizResults.Add(new QuizResult
        {
            TenantId = @event.TenantId,
            UserId = @event.UserId,
            QuizId = @event.QuizId,
            TopicId = @event.TopicId,
            QuizTitle = @event.QuizTitle,
            Score = @event.Score,
            Total = @event.Total,
            SubmittedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);

        var topicScope = await _scope.GetTopicScopeAsync(@event.TopicId, cancellationToken);
        if (topicScope is not null)
            await _certificates.TryIssueIfCompleteAsync(@event.UserId, topicScope.BundleId, cancellationToken);
    }
}
