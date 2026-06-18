using Lms.Modules.LiveClasses.Infrastructure;
using Lms.Shared.LiveClasses;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.LiveClasses.Application;

public sealed class LiveClassReminderReader : ILiveClassReminderReader
{
    private readonly LiveClassesDbContext _db;

    public LiveClassReminderReader(LiveClassesDbContext db) => _db = db;

    public async Task<IReadOnlyList<LiveClassReminderCandidate>> GetPendingRemindersAsync(
        DateTime windowStartUtc,
        DateTime windowEndUtc,
        CancellationToken ct = default) =>
        await _db.LiveClasses.IgnoreQueryFilters().AsNoTracking()
            .Where(c => !c.IsCancelled
                        && c.ReminderNotifiedAtUtc == null
                        && c.ScheduledStartUtc >= windowStartUtc
                        && c.ScheduledStartUtc <= windowEndUtc)
            .Select(c => new LiveClassReminderCandidate(
                c.Id,
                c.TenantId,
                c.BundleId,
                c.Title,
                c.SubjectTitle,
                c.ScheduledStartUtc,
                c.JoinUrl))
            .ToListAsync(ct);

    public async Task MarkReminderSentAsync(Guid liveClassId, CancellationToken ct = default)
    {
        var row = await _db.LiveClasses.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == liveClassId, ct);
        if (row is null) return;

        row.ReminderNotifiedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
