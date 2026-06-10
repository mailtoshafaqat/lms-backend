using Lms.Shared.Content;
using Lms.Shared.Events;
using Microsoft.Extensions.Logging;

namespace Lms.Modules.SyllabusMentor.Application;

public sealed class NoteIngestHandler : IEventHandler<NoteContentChangedEvent>
{
    private readonly ISyllabusMentorService _mentor;
    private readonly ILogger<NoteIngestHandler> _logger;

    public NoteIngestHandler(ISyllabusMentorService mentor, ILogger<NoteIngestHandler> logger)
    {
        _mentor = mentor;
        _logger = logger;
    }

    public async Task HandleAsync(NoteContentChangedEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            await _mentor.IngestAsync(new IngestRequest(@event.TopicId, null), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Auto-ingest failed for topic {TopicId}", @event.TopicId);
        }
    }
}
