namespace Lms.Modules.SyllabusMentor.Application;

public interface ISyllabusMentorService
{
    Task<AskResponse> AskAsync(Guid userId, string role, AskRequest request, CancellationToken ct = default);
    Task<IngestResponse> IngestAsync(IngestRequest request, CancellationToken ct = default);
}
