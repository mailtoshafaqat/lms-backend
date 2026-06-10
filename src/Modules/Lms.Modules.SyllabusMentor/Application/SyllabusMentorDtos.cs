namespace Lms.Modules.SyllabusMentor.Application;

public sealed record AskRequest(
    string Question,
    Guid? TopicId,
    Guid? SubjectId,
    string Language = "en");

public sealed record CitationDto(
    string SourceType,
    string SourceTitle,
    Guid? TopicId,
    string Excerpt);

public sealed record AskResponse(
    string Answer,
    string Language,
    string ScopeLabel,
    bool SyllabusLocked,
    IReadOnlyList<CitationDto> Citations);

public sealed record IngestRequest(Guid? TopicId, Guid? SubjectId, bool ReindexAll = false);

public sealed record IngestResponse(int ChunksIndexed, string Message);
