using Lms.Shared.Email;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Platform.Infrastructure;

public sealed class BrandedEmailRenderer : IBrandedEmailRenderer
{
    private readonly PlatformDbContext _db;

    public BrandedEmailRenderer(PlatformDbContext db) => _db = db;

    public async Task<string> RenderAsync(
        Guid tenantId, string subject, string bodyHtml, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        var settings = await _db.TenantSettings.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId, ct);

        var name = !string.IsNullOrWhiteSpace(settings?.DisplayName)
            ? settings!.DisplayName
            : tenant?.Name ?? "Your institute";
        var color = string.IsNullOrWhiteSpace(settings?.PrimaryColor) ? "#0b3d91" : settings!.PrimaryColor;
        var logo = settings?.LogoUrl;
        var logoImg = !string.IsNullOrWhiteSpace(logo)
            ? $"<img src=\"{logo}\" alt=\"\" style=\"height:40px;margin-bottom:12px\" />"
            : "";

        return $"""
            <div style="font-family:system-ui,sans-serif;max-width:560px;margin:0 auto;color:#1e293b">
              <div style="border-bottom:3px solid {color};padding-bottom:16px;margin-bottom:20px">
                {logoImg}
                <div style="font-size:18px;font-weight:700;color:{color}">{name}</div>
              </div>
              <div style="font-size:15px;line-height:1.6">{bodyHtml}</div>
              <p style="margin-top:32px;font-size:12px;color:#64748b">
                {name} · This is an automated message.
              </p>
            </div>
            """;
    }
}
