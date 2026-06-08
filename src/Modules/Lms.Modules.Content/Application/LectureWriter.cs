using Lms.Modules.Content.Domain;
using Lms.Modules.Content.Infrastructure;
using Lms.Shared.Content;
using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Content.Application;

public sealed class LectureWriter : ILectureWriter
{
    private readonly ContentDbContext _db;
    private readonly ITenantContext _tenant;

    public LectureWriter(ContentDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<Guid> UpsertMembersOnlyLectureAsync(
        Guid topicId, string title, string recordingUrl, Guid? liveClassId, CancellationToken ct = default)
    {
        var existing = liveClassId is null
            ? null
            : await _db.Lectures.FirstOrDefaultAsync(
                l => l.SourceLiveClassId == liveClassId, ct);

        if (existing is null)
        {
            var maxOrder = await _db.Lectures.Where(l => l.TopicId == topicId)
                .Select(l => (int?)l.Order).MaxAsync(ct) ?? 0;

            existing = new Lecture
            {
                TenantId = _tenant.TenantId,
                TopicId = topicId,
                Title = title.Trim(),
                Url = recordingUrl.Trim(),
                MembersOnly = true,
                SourceLiveClassId = liveClassId,
                Order = maxOrder + 1
            };
            _db.Lectures.Add(existing);
        }
        else
        {
            existing.Title = title.Trim();
            existing.Url = recordingUrl.Trim();
            existing.TopicId = topicId;
            existing.MembersOnly = true;
        }

        await _db.SaveChangesAsync(ct);
        return existing.Id;
    }
}
