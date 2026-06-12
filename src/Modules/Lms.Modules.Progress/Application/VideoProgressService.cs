using Lms.Modules.Progress.Domain;
using Lms.Modules.Progress.Infrastructure;
using Lms.Shared.Courses;
using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Progress.Application;

public sealed class VideoProgressService : IVideoProgressService
{
    private readonly ProgressDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICourseScopeReader _scope;
    private readonly ICertificateService _certificates;

    public VideoProgressService(
        ProgressDbContext db,
        ITenantContext tenant,
        ICourseScopeReader scope,
        ICertificateService certificates)
    {
        _db = db;
        _tenant = tenant;
        _scope = scope;
        _certificates = certificates;
    }

    public async Task<LectureProgressDto> SaveProgressAsync(
        Guid userId, Guid lectureId, SaveLectureProgressRequest request, CancellationToken ct = default)
    {
        var percent = Math.Clamp(request.ProgressPercent, 0, 100);
        var position = Math.Max(0, request.PositionSec);

        var topicId = request.TopicId;
        if (topicId is null || topicId == Guid.Empty)
        {
            var existing = await _db.LectureWatchProgress
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId && p.LectureId == lectureId, ct);
            topicId = existing?.TopicId;
        }

        if (topicId is null || topicId == Guid.Empty)
            throw new InvalidOperationException("TopicId is required for first progress save.");

        var row = await _db.LectureWatchProgress
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.LectureId == lectureId, ct);

        if (row is null)
        {
            row = new LectureWatchProgress
            {
                TenantId = _tenant.TenantId,
                UserId = userId,
                LectureId = lectureId,
                TopicId = topicId.Value,
                ProgressPercent = percent,
                PositionSec = position,
                LastWatchedAt = DateTime.UtcNow
            };
            _db.LectureWatchProgress.Add(row);
        }
        else
        {
            if (row.TenantId != _tenant.TenantId)
                row.TenantId = _tenant.TenantId;

            row.ProgressPercent = Math.Max(row.ProgressPercent, percent);
            row.PositionSec = Math.Max(row.PositionSec, position);
            row.LastWatchedAt = DateTime.UtcNow;
            row.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);

        var topicScope = await _scope.GetTopicScopeAsync(topicId.Value, ct);
        if (topicScope is not null)
            await _certificates.TryIssueIfCompleteAsync(userId, topicScope.BundleId, ct);

        return Map(row);
    }

    public async Task<LectureProgressDto?> GetProgressAsync(
        Guid userId, Guid lectureId, CancellationToken ct = default)
    {
        var row = await _db.LectureWatchProgress.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.LectureId == lectureId, ct);
        return row is null ? null : Map(row);
    }

    public async Task<IReadOnlyList<LectureProgressDto>> GetProgressForLecturesAsync(
        Guid userId, IReadOnlyList<Guid> lectureIds, CancellationToken ct = default)
    {
        if (lectureIds.Count == 0) return [];

        var idSet = lectureIds.Distinct().ToList();
        var rows = await _db.LectureWatchProgress.AsNoTracking()
            .Where(p => p.UserId == userId && idSet.Contains(p.LectureId))
            .ToListAsync(ct);

        return rows.Select(Map).ToList();
    }

    private static LectureProgressDto Map(LectureWatchProgress row) => new(
        row.LectureId,
        row.TopicId,
        row.ProgressPercent,
        row.PositionSec,
        row.LastWatchedAt);
}
