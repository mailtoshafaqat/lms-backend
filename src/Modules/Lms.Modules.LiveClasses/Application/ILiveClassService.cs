using Lms.Shared.Common;

namespace Lms.Modules.LiveClasses.Application;

public interface ILiveClassService
{
    /// <summary>Upcoming and live classes for the courses the user is currently enrolled in.</summary>
    Task<IReadOnlyList<LiveClassDto>> GetForUserAsync(Guid userId, CancellationToken ct = default);

    Task<IReadOnlyList<AdminLiveClassDto>> ListAsync(CancellationToken ct = default);
    Task<Result<AdminLiveClassDto>> CreateAsync(Guid createdByUserId, CreateLiveClassRequest request, CancellationToken ct = default);
    Task<bool> CancelAsync(Guid id, CancellationToken ct = default);

    /// <summary>Whether Zoom auto-creation is available for the current tenant (drives admin UI hints).</summary>
    Task<bool> IsZoomConfiguredAsync(CancellationToken ct = default);

    Task<Result<AdminLiveClassDto>> AttachRecordingAsync(
        Guid id, AttachRecordingRequest request, CancellationToken ct = default);
}
