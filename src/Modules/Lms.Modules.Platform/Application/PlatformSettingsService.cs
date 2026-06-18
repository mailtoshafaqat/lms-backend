using Lms.Modules.Platform.Domain;
using Lms.Modules.Platform.Infrastructure;
using Lms.Shared.Payments;
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

    public async Task<PaymentSettingsDto> GetPaymentSettingsAsync(CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == _tenant.TenantId, ct);
        var s = await _db.TenantSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        return MapPayments(tenant, s);
    }

    public async Task<PaymentSettingsDto> UpdatePaymentSettingsAsync(
        UpdatePaymentSettingsRequest request, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == _tenant.TenantId, ct)
            ?? throw new InvalidOperationException("Tenant not found.");
        var s = await GetOrCreateAsync(ct);

        tenant.EnrollmentModes = (EnrollmentModes)request.EnrollmentModes;
        s.ManualPaymentInstructions = string.IsNullOrWhiteSpace(request.ManualPaymentInstructions)
            ? null
            : request.ManualPaymentInstructions.Trim();

        var allowed = tenant.AllowedPaymentGateways;

        s.ManualPaymentEnabled = request.ManualEnabled && allowed.HasFlag(PaymentGatewayFlags.Manual);
        s.StripeEnabled = request.StripeEnabled && allowed.HasFlag(PaymentGatewayFlags.Stripe);
        s.StripePublishableKey = request.StripePublishableKey.Trim();
        if (!string.IsNullOrWhiteSpace(request.StripeSecretKey))
            s.StripeSecretKey = request.StripeSecretKey;
        if (!string.IsNullOrWhiteSpace(request.StripeWebhookSecret))
            s.StripeWebhookSecret = request.StripeWebhookSecret;

        s.JazzCashEnabled = request.JazzCashEnabled && allowed.HasFlag(PaymentGatewayFlags.JazzCash);
        s.JazzCashMerchantId = request.JazzCashMerchantId.Trim();
        if (!string.IsNullOrWhiteSpace(request.JazzCashPassword))
            s.JazzCashPassword = request.JazzCashPassword;
        if (!string.IsNullOrWhiteSpace(request.JazzCashHashKey))
            s.JazzCashHashKey = request.JazzCashHashKey;
        s.JazzCashReturnUrl = string.IsNullOrWhiteSpace(request.JazzCashReturnUrl)
            ? null
            : request.JazzCashReturnUrl.Trim();

        s.EasypaisaEnabled = request.EasypaisaEnabled && allowed.HasFlag(PaymentGatewayFlags.Easypaisa);
        s.EasypaisaStoreId = request.EasypaisaStoreId.Trim();
        if (!string.IsNullOrWhiteSpace(request.EasypaisaHashKey))
            s.EasypaisaHashKey = request.EasypaisaHashKey;
        if (!string.IsNullOrWhiteSpace(request.EasypaisaCredentials))
            s.EasypaisaCredentials = request.EasypaisaCredentials;

        await _db.SaveChangesAsync(ct);
        return MapPayments(tenant, s);
    }

    public async Task<EnrollmentSettingsDto> GetEnrollmentSettingsAsync(CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == _tenant.TenantId, ct);
        return new EnrollmentSettingsDto(tenant?.AllowStudentSelfEnroll ?? false);
    }

    public async Task<EnrollmentSettingsDto> UpdateEnrollmentSettingsAsync(
        UpdateEnrollmentSettingsRequest request, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == _tenant.TenantId, ct)
            ?? throw new InvalidOperationException("Tenant not found.");

        tenant.AllowStudentSelfEnroll = request.AllowStudentSelfEnroll;
        await _db.SaveChangesAsync(ct);
        return new EnrollmentSettingsDto(tenant.AllowStudentSelfEnroll);
    }

    public async Task<BrandingDto?> GetPublicBrandingAsync(string slug, CancellationToken ct = default)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        var tenant = await _db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Slug == normalized && t.Status != TenantStatus.Suspended, ct);
        if (tenant is null) return null;

        var s = await _db.TenantSettings.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenant.Id, ct);

        return MapBranding(tenant, s);
    }

    public async Task<BrandingDto> GetBrandingAsync(CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == _tenant.TenantId, ct);
        var s = await _db.TenantSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        return MapBranding(tenant, s)!;
    }

    public async Task<BrandingDto> UpdateBrandingAsync(UpdateBrandingRequest request, CancellationToken ct = default)
    {
        var s = await GetOrCreateAsync(ct);
        ApplyBranding(s, request);
        await _db.SaveChangesAsync(ct);

        var tenant = await _db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == _tenant.TenantId, ct);
        return MapBranding(tenant, s)!;
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
        return MapBranding(tenant, s)!;
    }

    private static void ApplyBranding(TenantSettings s, UpdateBrandingRequest request)
    {
        s.DisplayName = request.DisplayName.Trim();
        s.LogoUrl = string.IsNullOrWhiteSpace(request.LogoUrl) ? null : request.LogoUrl.Trim();
        s.FaviconUrl = string.IsNullOrWhiteSpace(request.FaviconUrl) ? null : request.FaviconUrl.Trim();
        s.PrimaryColor = string.IsNullOrWhiteSpace(request.PrimaryColor) ? "#0b3d91" : request.PrimaryColor.Trim();
        s.SupportEmail = string.IsNullOrWhiteSpace(request.SupportEmail) ? null : request.SupportEmail.Trim();
        s.MentorDisplayName = string.IsNullOrWhiteSpace(request.MentorDisplayName)
            ? null
            : request.MentorDisplayName.Trim();
    }

    private static BrandingDto? MapBranding(Tenant? tenant, TenantSettings? s)
    {
        var slug = tenant?.Slug ?? "demo";
        var fallbackName = tenant?.Name ?? "Institute";
        var display = !string.IsNullOrWhiteSpace(s?.DisplayName) ? s!.DisplayName : fallbackName;
        var mentorName = !string.IsNullOrWhiteSpace(s?.MentorDisplayName)
            ? s!.MentorDisplayName!
            : $"{display} Mentor";
        return new BrandingDto(
            slug,
            display,
            s?.LogoUrl,
            s?.FaviconUrl,
            string.IsNullOrWhiteSpace(s?.PrimaryColor) ? "#0b3d91" : s!.PrimaryColor,
            s?.SupportEmail,
            mentorName,
            tenant?.SyllabusMentorEnabled ?? true,
            tenant?.BundlePriceEditEnabled ?? true,
            tenant?.McqBulkImportEnabled ?? true,
            tenant?.AllowStudentSelfEnroll ?? false);
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

    private static PaymentSettingsDto MapPayments(Tenant? tenant, TenantSettings? s)
    {
        var modes = tenant?.EnrollmentModes ?? EnrollmentModes.AdminOnly;
        return new PaymentSettingsDto(
            (int)modes,
            s?.ManualPaymentInstructions,
            new ManualGatewaySettingsDto(s?.ManualPaymentEnabled ?? false),
            new StripeGatewaySettingsDto(
                s?.StripeEnabled ?? false,
                s?.StripePublishableKey ?? string.Empty,
                !string.IsNullOrEmpty(s?.StripeSecretKey),
                !string.IsNullOrEmpty(s?.StripeWebhookSecret)),
            new JazzCashGatewaySettingsDto(
                s?.JazzCashEnabled ?? false,
                s?.JazzCashMerchantId ?? string.Empty,
                !string.IsNullOrEmpty(s?.JazzCashPassword),
                !string.IsNullOrEmpty(s?.JazzCashHashKey),
                s?.JazzCashReturnUrl),
            new EasypaisaGatewaySettingsDto(
                s?.EasypaisaEnabled ?? false,
                s?.EasypaisaStoreId ?? string.Empty,
                !string.IsNullOrEmpty(s?.EasypaisaHashKey),
                !string.IsNullOrEmpty(s?.EasypaisaCredentials)));
    }

    private static EmailSettingsDto Empty() =>
        new(false, string.Empty, string.Empty, string.Empty, 587, string.Empty, false, true);

    private static EmailSettingsDto Map(TenantSettings s) =>
        new(s.EmailEnabled, s.FromEmail, s.FromName, s.SmtpHost, s.SmtpPort, s.SmtpUser,
            !string.IsNullOrEmpty(s.SmtpPassword), s.UseSsl);
}
