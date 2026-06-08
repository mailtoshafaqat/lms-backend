using Lms.Modules.Courses.Contracts;
using Lms.Modules.LiveClasses.Domain;
using Lms.Modules.LiveClasses.Infrastructure;
using Lms.Shared.Common;
using Lms.Shared.Content;
using Lms.Shared.Enrollments;
using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.LiveClasses.Application;

public sealed class LiveClassService : ILiveClassService
{
    private readonly LiveClassesDbContext _db;
    private readonly IBundleCatalog _catalog;
    private readonly IEnrollmentReader _enrollments;
    private readonly IZoomMeetingService _zoom;
    private readonly ITenantContext _tenant;
    private readonly ITenantFeaturesProvider _features;
    private readonly ILectureWriter _lectures;

    public LiveClassService(
        LiveClassesDbContext db,
        IBundleCatalog catalog,
        IEnrollmentReader enrollments,
        IZoomMeetingService zoom,
        ITenantContext tenant,
        ITenantFeaturesProvider features,
        ILectureWriter lectures)
    {
        _db = db;
        _catalog = catalog;
        _enrollments = enrollments;
        _zoom = zoom;
        _tenant = tenant;
        _features = features;
        _lectures = lectures;
    }

    public async Task<IReadOnlyList<LiveClassDto>> GetForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var flags = await _features.GetAsync(_tenant.TenantId, ct);
        if (flags is not null && !flags.LiveClassesEnabled)
            return [];

        var bundleIds = await _enrollments.GetActiveBundleIdsAsync(userId, ct);
        if (bundleIds.Count == 0) return [];

        var now = DateTime.UtcNow;
        var rows = await _db.LiveClasses
            .Where(c => bundleIds.Contains(c.BundleId) && !c.IsCancelled)
            .Where(c =>
                c.ScheduledStartUtc.AddMinutes(c.DurationMinutes) > now
                || c.RecordingUrl != null)
            .OrderByDescending(c => c.RecordingUrl != null)
            .ThenBy(c => c.ScheduledStartUtc)
            .ToListAsync(ct);

        return rows.Select(MapStudent).ToList();
    }

    public async Task<IReadOnlyList<AdminLiveClassDto>> ListAsync(CancellationToken ct = default)
    {
        var rows = await _db.LiveClasses
            .OrderByDescending(c => c.ScheduledStartUtc)
            .ToListAsync(ct);

        return rows.Select(MapAdmin).ToList();
    }

    public async Task<Result<AdminLiveClassDto>> CreateAsync(
        Guid createdByUserId, CreateLiveClassRequest request, CancellationToken ct = default)
    {
        var flags = await _features.GetAsync(_tenant.TenantId, ct);
        if (flags is not null && !flags.LiveClassesEnabled)
            return Result<AdminLiveClassDto>.Failure("Live classes are disabled for this institute.");

        if (string.IsNullOrWhiteSpace(request.Title))
            return Result<AdminLiveClassDto>.Failure("Title is required.");
        if (request.DurationMinutes <= 0)
            return Result<AdminLiveClassDto>.Failure("Duration must be greater than zero.");

        var bundle = await _catalog.GetBundleAsync(request.BundleId, ct);
        if (bundle is null)
            return Result<AdminLiveClassDto>.Failure("Course not found.");

        var entity = new LiveClass
        {
            TenantId = _tenant.TenantId,
            BundleId = bundle.Id,
            BundleTitle = bundle.Title,
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            ScheduledStartUtc = DateTime.SpecifyKind(request.ScheduledStartUtc, DateTimeKind.Utc),
            DurationMinutes = request.DurationMinutes,
            CreatedByUserId = createdByUserId
        };

        try
        {
            var meeting = await _zoom.CreateMeetingAsync(
                entity.Title, entity.ScheduledStartUtc, entity.DurationMinutes, ct);

            if (meeting is not null)
            {
                entity.Provider = LiveClassProvider.Zoom;
                entity.JoinUrl = meeting.JoinUrl;
                entity.StartUrl = meeting.StartUrl;
                entity.MeetingId = meeting.MeetingId;
                entity.Passcode = meeting.Passcode;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(request.ManualJoinUrl))
                    return Result<AdminLiveClassDto>.Failure(
                        "Zoom is not configured. Provide a join link or set up Zoom in settings.");

                entity.Provider = LiveClassProvider.Manual;
                entity.JoinUrl = request.ManualJoinUrl.Trim();
            }
        }
        catch (Exception ex)
        {
            return Result<AdminLiveClassDto>.Failure($"Could not create the Zoom meeting: {ex.Message}");
        }

        _db.LiveClasses.Add(entity);
        await _db.SaveChangesAsync(ct);

        return Result<AdminLiveClassDto>.Success(MapAdmin(entity));
    }

    public async Task<bool> CancelAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.LiveClasses.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (entity is null) return false;

        entity.IsCancelled = true;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public Task<bool> IsZoomConfiguredAsync(CancellationToken ct = default) => _zoom.IsConfiguredAsync(ct);

    public async Task<Result<AdminLiveClassDto>> AttachRecordingAsync(
        Guid id, AttachRecordingRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.RecordingUrl))
            return Result<AdminLiveClassDto>.Failure("Recording URL is required.");

        var entity = await _db.LiveClasses.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (entity is null) return Result<AdminLiveClassDto>.Failure("Live class not found.");

        var title = string.IsNullOrWhiteSpace(request.LectureTitle)
            ? $"{entity.Title} (Recording)"
            : request.LectureTitle.Trim();

        var lectureId = await _lectures.UpsertMembersOnlyLectureAsync(
            request.TopicId, title, request.RecordingUrl.Trim(), entity.Id, ct);

        entity.RecordingUrl = request.RecordingUrl.Trim();
        entity.RecordingTopicId = request.TopicId;
        entity.RecordingLectureId = lectureId;
        await _db.SaveChangesAsync(ct);

        return Result<AdminLiveClassDto>.Success(MapAdmin(entity));
    }

    private static LiveClassState ComputeState(LiveClass c)
    {
        if (c.IsCancelled) return LiveClassState.Cancelled;
        var now = DateTime.UtcNow;
        var end = c.ScheduledStartUtc.AddMinutes(c.DurationMinutes);
        if (now < c.ScheduledStartUtc) return LiveClassState.Upcoming;
        return now <= end ? LiveClassState.Live : LiveClassState.Ended;
    }

    private static LiveClassDto MapStudent(LiveClass c) => new(
        c.Id, c.BundleId, c.BundleTitle, c.Title, c.Description,
        c.ScheduledStartUtc, c.DurationMinutes, ComputeState(c).ToString(),
        c.Provider.ToString(), c.JoinUrl, c.Passcode, c.RecordingUrl);

    private static AdminLiveClassDto MapAdmin(LiveClass c) => new(
        c.Id, c.BundleId, c.BundleTitle, c.Title, c.Description,
        c.ScheduledStartUtc, c.DurationMinutes, ComputeState(c).ToString(),
        c.Provider.ToString(), c.JoinUrl, c.StartUrl, c.MeetingId, c.Passcode,
        c.RecordingUrl, c.RecordingTopicId, c.RecordingLectureId);
}
