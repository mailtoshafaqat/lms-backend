using Lms.Shared.Entities;

namespace Lms.Modules.SyllabusMentor.Domain;

public sealed class KnowledgeChunk : TenantEntity
{
    public Guid? TopicId { get; set; }
    public Guid? SubjectId { get; set; }
    public string SourceType { get; set; } = "note";
    public Guid SourceId { get; set; }
    public string SourceTitle { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
}
