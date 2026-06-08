using Lms.Modules.Platform.Domain;
using Lms.Modules.Platform.Infrastructure;
using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Platform.Application;

public sealed class PlatformSettingsService : IPlatformSettingsService
{
    private readonly PlatformDbContext _db;
    private readonly ITenantContext _tenant;

    public PlatformSettingsService(PlatformDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<EmailSettingsDto> GetEmailSettingsAsync(CancellationToken ct = default)
    {
        var s = await _db.TenantSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        return s is null ? Empty() : Map(s);
    }

    public async Task<EmailSettingsDto> UpdateEmailSettingsAsync(
        UpdateEmailSettingsRequest request, CancellationToken ct = default)
    {
        var s = await GetOrCreateAsync(ct);

        s.EmailEnabled = request.Enabled;
        s.FromEmail = request.FromEmail.Trim();
        s.FromName = request.FromName.Trim();
        s.SmtpHost = request.SmtpHost.Trim();
        s.SmtpPort = request.SmtpPort;
        s.SmtpUser = request.SmtpUser.Trim();
        s.UseSsl = request.UseSsl;

        // Keep the existing password when the field is left blank.
        if (!string.IsNullOrWhiteSpace(request.SmtpPassword))
            s.SmtpPassword = request.SmtpPassword;

        await _db.SaveChangesAsync(ct);
        return Map(s);
    }

    public async Task<ZoomSettingsDto> GetZoomSettingsAsync(CancellationToken ct = default)
    {
        var s = await _db.TenantSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        return s is null ? new ZoomSettingsDto(false, string.Empty, string.Empty, false) : MapZoom(s);
    }

    public async Task<ZoomSettingsDto> UpdateZoomSettingsAsync(
        UpdateZoomSettingsRequest request, CancellationToken ct = default)
    {
        var s = await GetOrCreateAsync(ct);

        s.ZoomEnabled = request.Enabled;
        s.ZoomAccountId = request.AccountId.Trim();
        s.ZoomClientId = request.ClientId.Trim();

        if (!string.IsNullOrWhiteSpace(request.ClientSecret))
            s.ZoomClientSecret = request.ClientSecret;

        await _db.SaveChangesAsync(ct);
        return MapZoom(s);
    }

    public async Task<BrandingDto?> GetPublicBrandingAsync(string slug, CancellationToken ct = default)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        var tenant = await _db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Slug == normalized && t.Status != TenantStatus.Suspended, ct);
        if (tenant is null) return null;

        var s = await _db.TenantSettings.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenant.Id, ct);

        return MapBranding(tenant.Slug, tenant.Name, s);
    }

    public async Task<BrandingDto> GetBrandingAsync(CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == _tenant.TenantId, ct);
        var s = await _db.TenantSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        var slug = tenant?.Slug ?? "demo";
        var name = tenant?.Name ?? "Institute";
        return MapBranding(slug, name, s)!;
    }

    public async Task<BrandingDto> UpdateBrandingAsync(UpdateBrandingRequest request, CancellationToken ct = default)
    {
        var s = await GetOrCreateAsync(ct);
        ApplyBranding(s, request);
        await _db.SaveChangesAsync(ct);

        var tenant = await _db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == _tenant.TenantId, ct);
        return MapBranding(tenant?.Slug ?? "demo", s.DisplayName, s)!;
    }

    public async Task<BrandingDto> UpdateTenantBrandingAsync(
        Guid tenantId, UpdateBrandingRequest request, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct)
            ?? throw new InvalidOperationException("Tenant not found.");

        var s = await _db.TenantSettings.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, ct);
        if (s is null)
        {
            s = new TenantSettings { TenantId = tenantId };
            _db.TenantSettings.Add(s);
        }

        ApplyBranding(s, request);
        if (string.IsNullOrWhiteSpace(s.DisplayName))
            s.DisplayName = tenant.Name;

        await _db.SaveChangesAsync(ct);
        return MapBranding(tenant.Slug, s.DisplayName, s)!;
    }

    private static void ApplyBranding(TenantSettings s, UpdateBrandingRequest request)
    {
        s.DisplayName = request.DisplayName.Trim();
        s.LogoUrl = string.IsNullOrWhiteSpace(request.LogoUrl) ? null : request.LogoUrl.Trim();
        s.FaviconUrl = string.IsNullOrWhiteSpace(request.FaviconUrl) ? null : request.FaviconUrl.Trim();
        s.PrimaryColor = string.IsNullOrWhiteSpace(request.PrimaryColor) ? "#0b3d91" : request.PrimaryColor.Trim();
        s.SupportEmail = string.IsNullOrWhiteSpace(request.SupportEmail) ? null : request.SupportEmail.Trim();
    }

    private static BrandingDto? MapBranding(string slug, string fallbackName, TenantSettings? s)
    {
        var display = !string.IsNullOrWhiteSpace(s?.DisplayName) ? s!.DisplayName : fallbackName;
        return new BrandingDto(
            slug,
            display,
            s?.LogoUrl,
            s?.FaviconUrl,
            string.IsNullOrWhiteSpace(s?.PrimaryColor) ? "#0b3d91" : s!.PrimaryColor,
            s?.SupportEmail);
    }

    private async Task<TenantSettings> GetOrCreateAsync(CancellationToken ct)
    {
        var s = await _db.TenantSettings.FirstOrDefaultAsync(ct);
        if (s is null)
        {
            var tenant = await _db.Tenants.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == _tenant.TenantId, ct);
            s = new TenantSettings
            {
                TenantId = _tenant.TenantId,
                DisplayName = tenant?.Name ?? string.Empty
            };
            _db.TenantSettings.Add(s);
        }
        return s;
    }

    private static ZoomSettingsDto MapZoom(TenantSettings s) =>
        new(s.ZoomEnabled, s.ZoomAccountId, s.ZoomClientId, !string.IsNullOrEmpty(s.ZoomClientSecret));

    private static EmailSettingsDto Empty() =>
        new(false, string.Empty, string.Empty, string.Empty, 587, string.Empty, false, true);

    private static EmailSettingsDto Map(TenantSettings s) =>
        new(s.EmailEnabled, s.FromEmail, s.FromName, s.SmtpHost, s.SmtpPort, s.SmtpUser,
            !string.IsNullOrEmpty(s.SmtpPassword), s.UseSsl);
}
