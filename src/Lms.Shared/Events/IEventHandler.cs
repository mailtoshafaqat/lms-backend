namespace Lms.Shared.Events;

/// <summary>A subscriber that reacts to an event. Modules register handlers for events
/// published by other modules — zero-to-many handlers, fully decoupled.</summary>
public interface IEventHandler<in TEvent> where TEvent : IEvent
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}
