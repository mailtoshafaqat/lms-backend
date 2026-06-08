namespace Lms.Shared.Events;

/// <summary>Publishes events to all registered handlers. Phase 1 uses an in-memory
/// implementation; a durable Outbox / message queue can replace it later without
/// changing publishers or subscribers.</summary>
public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IEvent;
}
