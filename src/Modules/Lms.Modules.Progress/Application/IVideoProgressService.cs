namespace Lms.Modules.Progress.Application;

public interface IVideoProgressService
{
    Task<LectureProgressDto> SaveProgressAsync(
        Guid userId, Guid lectureId, SaveLectureProgressRequest request, CancellationToken ct = default);

    Task<LectureProgressDto?> GetProgressAsync(
        Guid userId, Guid lectureId, CancellationToken ct = default);

    Task<IReadOnlyList<LectureProgressDto>> GetProgressForLecturesAsync(
        Guid userId, IReadOnlyList<Guid> lectureIds, CancellationToken ct = default);
}
