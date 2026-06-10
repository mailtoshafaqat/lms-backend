using Lms.Shared.Events;

namespace Lms.Shared.Content;

/// <summary>Published when topic notes change. Syllabus Mentor re-indexes the topic.</summary>
public sealed record NoteContentChangedEvent(Guid TenantId, Guid TopicId) : IEvent;
