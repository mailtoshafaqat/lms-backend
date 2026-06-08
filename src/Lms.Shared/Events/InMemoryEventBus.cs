using Microsoft.Extensions.DependencyInjection;

namespace Lms.Shared.Events;

/// <summary>In-process event delivery (Phase 1). Resolves all <see cref="IEventHandler{TEvent}"/>
/// for the published event type and invokes them. No external infrastructure required.</summary>
public sealed class InMemoryEventBus : IEventBus
{
    private readonly IServiceProvider _provider;

    public InMemoryEventBus(IServiceProvider provider) => _provider = provider;

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IEvent
    {
        var handlers = _provider.GetServices<IEventHandler<TEvent>>();
        foreach (var handler in handlers)
        {
            await handler.HandleAsync(@event, cancellationToken);
        }
    }
}
