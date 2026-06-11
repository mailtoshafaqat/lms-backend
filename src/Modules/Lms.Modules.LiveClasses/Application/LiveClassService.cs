using Lms.Modules.Courses.Contracts;
using Lms.Modules.LiveClasses.Domain;
using Lms.Modules.LiveClasses.Infrastructure;
using Lms.Shared.Auth;
using Lms.Shared.Common;
using Lms.Shared.Content;
using Lms.Shared.Courses;
using Lms.Shared.Enrollments;
using Lms.Shared.Tenancy;
using Lms.Shared.Users;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.LiveClasses.Application;

public sealed class LiveClassService : ILiveClassService
{
    private readonly LiveClassesDbContext _db;
    private readonly IBundleCatalog _catalog;
    private readonly ICourseScopeReader _scope;
    private readonly ISubjectAccessService _subjects;
    private readonly IInstituteUserReader _users;
    private readonly IUserDirectory _userDirectory;
    private readonly IEnrollmentReader _enrollments;
    private readonly IZoomMeetingService _zoom;
    private readonly ITenantContext _tenant;
    private readonly ITenantFeaturesProvider _features;
    private readonly ILectureWriter _lectures;

    public LiveClassService(
        LiveClassesDbContext db,
        IBundleCatalog catalog,
        ICourseScopeReader scope,
        ISubjectAccessService subjects,
        IInstituteUserReader users,
        IUserDirectory userDirectory,
        IEnrollmentReader enrollments,
        IZoomMeetingService zoom,
        ITenantContext tenant,
        ITenantFeaturesProvider features,
        ILectureWriter lectures)
    {
        _db = db;
        _catalog = catalog;
        _scope = scope;
        _subjects = subjects;
        _users = users;
        _userDirectory = userDirectory;
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

    public async Task<PagedResult<AdminLiveClassDto>> ListAsync(
        Guid userId, string role, PagedListQuery query, string? stateFilter, CancellationToken ct = default)
    {
        var q = _db.LiveClasses.AsQueryable();

        if (!_subjects.HasInstituteWideAccess(role))
        {
            if (role != Roles.Teacher)
                return new PagedResult<AdminLiveClassDto>([], query.NormalizedPage, query.NormalizedPageSize, 0);

            var assigned = await _subjects.GetAssignedSubjectsAsync(userId, role, ct);
            var subjectIds = assigned.Select(s => s.SubjectId).ToList();
            q = q.Where(c => c.HostUserId == userId || subjectIds.Contains(c.SubjectId));
        }

        q = ApplyStateFilter(q, stateFilter);

        if (query.NormalizedSearch is { } term)
        {
            var lower = term.ToLowerInvariant();
            q = q.Where(c => c.Title.ToLower().Contains(lower));
        }

        q = ApplyLiveClassSort(q, query, stateFilter);

        var page = query.NormalizedPage;
        var pageSize = query.NormalizedPageSize;
        var total = await q.CountAsync(ct);
        var rows = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var data = rows.Select(MapAdmin).ToList();

        return new PagedResult<AdminLiveClassDto>(data, page, pageSize, total);
    }

    public async Task<Result<AdminLiveClassDto>> CreateAsync(
        Guid createdByUserId,
        string role,
        CreateLiveClassRequest request,
        CancellationToken ct = default)
    {
        var flags = await _features.GetAsync(_tenant.TenantId, ct);
        if (flags is not null && !flags.LiveClassesEnabled)
            return Result<AdminLiveClassDto>.Failure("Live classes are disabled for this institute.");

        if (string.IsNullOrWhiteSpace(request.Title))
            return Result<AdminLiveClassDto>.Failure("Title is required.");
        if (request.DurationMinutes <= 0)
            return Result<AdminLiveClassDto>.Failure("Duration must be greater than zero.");
        if (request.SubjectId == Guid.Empty)
            return Result<AdminLiveClassDto>.Failure("Subject is required.");
        if (request.HostUserId == Guid.Empty)
            return Result<AdminLiveClassDto>.Failure("Host teacher is required.");

        if (role == Roles.Teacher)
        {
            if (request.HostUserId != createdByUserId)
                return Result<AdminLiveClassDto>.Failure("Teachers can only host their own live classes.");
            if (!await _subjects.CanManageSubjectAsync(createdByUserId, role, request.SubjectId, ct))
                return Result<AdminLiveClassDto>.Failure("You are not assigned to this subject.");
        }
        else if (!_subjects.HasInstituteWideAccess(role))
        {
            return Result<AdminLiveClassDto>.Failure("You are not allowed to schedule live classes.");
        }

        if (!await _users.IsActiveTeacherAsync(request.HostUserId, ct))
            return Result<AdminLiveClassDto>.Failure("Host must be an active teacher.");

        if (!await _subjects.IsTeacherAssignedAsync(request.HostUserId, request.SubjectId, ct))
            return Result<AdminLiveClassDto>.Failure("The host teacher is not assigned to this subject.");

        var subject = await _scope.GetSubjectScopeAsync(request.SubjectId, ct);
        if (subject is null)
            return Result<AdminLiveClassDto>.Failure("Subject not found.");

        var bundle = await _catalog.GetBundleAsync(subject.BundleId, ct);
        if (bundle is null)
            return Result<AdminLiveClassDto>.Failure("Course not found.");

        var hostNames = await _users.GetTeacherDisplayNamesAsync([request.HostUserId], ct);
        var hostName = hostNames.GetValueOrDefault(request.HostUserId, "Teacher");

        var entity = new LiveClass
        {
            TenantId = _tenant.TenantId,
            BundleId = bundle.Id,
            BundleTitle = bundle.Title,
            SubjectId = subject.SubjectId,
            SubjectTitle = subject.SubjectTitle,
            HostUserId = request.HostUserId,
            HostName = hostName,
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            ScheduledStartUtc = request.ScheduledStartUtc,
            DurationMinutes = request.DurationMinutes,
            CreatedByUserId = createdByUserId,
        };

        try
        {
            if (await _zoom.IsConfiguredAsync(ct))
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
            }
        }
        catch (Exception ex)
        {
            return Result<AdminLiveClassDto>.Failure($"Could not create the Zoom meeting: {ex.Message}");
        }

        if (entity.Provider != LiveClassProvider.Zoom)
        {
            var manual = request.ManualJoinUrl?.Trim();
            if (string.IsNullOrWhiteSpace(manual))
                return Result<AdminLiveClassDto>.Failure("A join URL is required when Zoom is not configured.");

            entity.Provider = LiveClassProvider.Manual;
            entity.JoinUrl = manual;
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

    public async Task<Result<RecordJoinResultDto>> RecordJoinAsync(
        Guid liveClassId, Guid userId, CancellationToken ct = default)
    {
        var liveClass = await _db.LiveClasses.FirstOrDefaultAsync(c => c.Id == liveClassId, ct);
        if (liveClass is null || liveClass.IsCancelled)
            return Result<RecordJoinResultDto>.Failure("Live class not found.");

        var state = ComputeState(liveClass);
        if (state != LiveClassState.Live)
            return state == LiveClassState.Upcoming
                ? Result<RecordJoinResultDto>.Failure("This class has not started yet. Join when it is live.")
                : Result<RecordJoinResultDto>.Failure("This class is not open for joining.");

        var bundleIds = await _enrollments.GetActiveBundleIdsAsync(userId, ct);
        if (!bundleIds.Contains(liveClass.BundleId))
            return Result<RecordJoinResultDto>.Failure("You are not enrolled in this course.");

        var names = await _userDirectory.GetDisplayNamesAsync([userId], ct);
        var userName = names.GetValueOrDefault(userId, "Student");

        var existing = await _db.Attendance
            .FirstOrDefaultAsync(a => a.LiveClassId == liveClassId && a.UserId == userId, ct);

        if (existing is null)
        {
            _db.Attendance.Add(new LiveClassAttendance
            {
                TenantId = _tenant.TenantId,
                LiveClassId = liveClassId,
                UserId = userId,
                UserName = userName,
                JoinedAtUtc = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(ct);
        }

        return Result<RecordJoinResultDto>.Success(new RecordJoinResultDto(liveClass.JoinUrl));
    }

    public async Task<Result<LiveClassAttendanceDto>> GetAttendanceAsync(
        Guid userId, string role, Guid liveClassId, CancellationToken ct = default)
    {
        var liveClass = await _db.LiveClasses.FirstOrDefaultAsync(c => c.Id == liveClassId, ct);
        if (liveClass is null)
            return Result<LiveClassAttendanceDto>.Failure("Live class not found.");

        if (!_subjects.HasInstituteWideAccess(role))
        {
            if (role != Roles.Teacher)
                return Result<LiveClassAttendanceDto>.Failure("Forbidden");

            var canManage = liveClass.HostUserId == userId
                || await _subjects.CanManageSubjectAsync(userId, role, liveClass.SubjectId, ct);
            if (!canManage)
                return Result<LiveClassAttendanceDto>.Failure("Forbidden");
        }

        var state = ComputeState(liveClass);
        if (state is not LiveClassState.Live and not LiveClassState.Ended)
            return Result<LiveClassAttendanceDto>.Failure("Attendance is only available for live or ended classes.");

        var rows = await _db.Attendance.AsNoTracking()
            .Where(a => a.LiveClassId == liveClassId)
            .OrderBy(a => a.JoinedAtUtc)
            .ToListAsync(ct);

        var attendees = rows
            .Select(a => new LiveClassAttendanceRowDto(a.UserId, a.UserName, a.JoinedAtUtc))
            .ToList();

        return Result<LiveClassAttendanceDto>.Success(new LiveClassAttendanceDto(
            liveClassId, liveClass.Title, attendees.Count, attendees));
    }

    private static IQueryable<LiveClass> ApplyStateFilter(IQueryable<LiveClass> query, string? stateFilter)
    {
        if (string.IsNullOrWhiteSpace(stateFilter) || stateFilter.Equals("all", StringComparison.OrdinalIgnoreCase))
            return query;

        var now = DateTime.UtcNow;

        if (stateFilter.Equals("cancelled", StringComparison.OrdinalIgnoreCase))
            return query.Where(c => c.IsCancelled);

        if (stateFilter.Equals("upcoming", StringComparison.OrdinalIgnoreCase))
            return query.Where(c => !c.IsCancelled && c.ScheduledStartUtc > now);

        if (stateFilter.Equals("live", StringComparison.OrdinalIgnoreCase))
            return query.Where(c =>
                !c.IsCancelled
                && c.ScheduledStartUtc <= now
                && c.ScheduledStartUtc.AddMinutes(c.DurationMinutes) >= now);

        if (stateFilter.Equals("ended", StringComparison.OrdinalIgnoreCase))
            return query.Where(c =>
                !c.IsCancelled
                && c.ScheduledStartUtc.AddMinutes(c.DurationMinutes) < now);

        return query;
    }

    private static IQueryable<LiveClass> ApplyLiveClassSort(
        IQueryable<LiveClass> query, PagedListQuery paging, string? stateFilter)
    {
        var sortBy = paging.SortBy?.Trim().ToLowerInvariant() ?? "scheduledstartutc";
        var nearestFirst = IsChronologicalAscendingState(stateFilter);

        return sortBy switch
        {
            "title" => PagedQueryExtensions.ResolveDescending(paging, false)
                ? query.OrderByDescending(c => c.Title)
                : query.OrderBy(c => c.Title),
            "bundle" or "bundletitle" => PagedQueryExtensions.ResolveDescending(paging, false)
                ? query.OrderByDescending(c => c.BundleTitle)
                : query.OrderBy(c => c.BundleTitle),
            "subject" or "subjecttitle" => PagedQueryExtensions.ResolveDescending(paging, false)
                ? query.OrderByDescending(c => c.SubjectTitle)
                : query.OrderBy(c => c.SubjectTitle),
            _ => nearestFirst
                ? query.OrderBy(c => c.ScheduledStartUtc)
                : PagedQueryExtensions.ResolveDescending(paging, true)
                    ? query.OrderByDescending(c => c.ScheduledStartUtc)
                    : query.OrderBy(c => c.ScheduledStartUtc),
        };
    }

    private static bool IsChronologicalAscendingState(string? stateFilter)
    {
        if (string.IsNullOrWhiteSpace(stateFilter) || stateFilter.Equals("all", StringComparison.OrdinalIgnoreCase))
            return false;

        return stateFilter.Equals("upcoming", StringComparison.OrdinalIgnoreCase)
            || stateFilter.Equals("live", StringComparison.OrdinalIgnoreCase);
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
        c.Id, c.BundleId, c.BundleTitle, c.SubjectId, c.SubjectTitle, c.HostUserId, c.HostName,
        c.Title, c.Description, c.ScheduledStartUtc, c.DurationMinutes, ComputeState(c).ToString(),
        c.Provider.ToString(), c.JoinUrl, c.Passcode, c.RecordingUrl);

    private static AdminLiveClassDto MapAdmin(LiveClass c) => new(
        c.Id, c.BundleId, c.BundleTitle, c.SubjectId, c.SubjectTitle, c.HostUserId, c.HostName,
        c.Title, c.Description, c.ScheduledStartUtc, c.DurationMinutes, ComputeState(c).ToString(),
        c.Provider.ToString(), c.JoinUrl, c.StartUrl, c.MeetingId, c.Passcode,
        c.RecordingUrl, c.RecordingTopicId, c.RecordingLectureId);
}
