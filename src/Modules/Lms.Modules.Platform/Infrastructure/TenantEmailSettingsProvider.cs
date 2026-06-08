using Lms.Shared.Email;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Platform.Infrastructure;

public sealed class TenantEmailSettingsProvider : ITenantEmailSettingsProvider
{
    private readonly PlatformDbContext _db;

    public TenantEmailSettingsProvider(PlatformDbContext db) => _db = db;

    public async Task<TenantEmailSettings?> GetAsync(CancellationToken ct = default)
    {
        var s = await _db.TenantSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        if (s is null) return null;

        return new TenantEmailSettings(
            s.EmailEnabled, s.FromEmail, s.FromName,
            s.SmtpHost, s.SmtpPort, s.SmtpUser, s.SmtpPassword, s.UseSsl);
    }
}
