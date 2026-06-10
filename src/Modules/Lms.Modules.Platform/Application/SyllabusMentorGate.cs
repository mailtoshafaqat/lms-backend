using Lms.Modules.Platform.Infrastructure;
using Lms.Shared.Mentor;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Platform.Application;

public sealed class SyllabusMentorGate : ISyllabusMentorGate
{
    private readonly PlatformDbContext _db;

    public SyllabusMentorGate(PlatformDbContext db) => _db = db;

    public async Task<SyllabusMentorConfig> GetConfigAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        if (tenant is null) return new SyllabusMentorConfig(false, "Syllabus Mentor");

        var settings = await _db.TenantSettings.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId, ct);

        var display = !string.IsNullOrWhiteSpace(settings?.DisplayName) ? settings!.DisplayName : tenant.Name;
        var mentorName = !string.IsNullOrWhiteSpace(settings?.MentorDisplayName)
            ? settings.MentorDisplayName!
            : $"{display} Mentor";

        return new SyllabusMentorConfig(tenant.SyllabusMentorEnabled, mentorName);
    }
}
