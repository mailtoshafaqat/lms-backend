namespace Lms.Modules.Content.Application;

public interface IContentAdminService
{
    Task<LectureDto> AddLectureAsync(Guid topicId, CreateLectureRequest req, CancellationToken ct = default);
    Task<NoteDto> AddNoteAsync(Guid topicId, CreateNoteRequest req, CancellationToken ct = default);
    Task<bool> DeleteLectureAsync(Guid id, CancellationToken ct = default);
    Task<bool> DeleteNoteAsync(Guid id, CancellationToken ct = default);
}
