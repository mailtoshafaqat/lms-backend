using Lms.Modules.Progress.Domain;
using Lms.Modules.Progress.Infrastructure;
using Lms.Shared.Branding;
using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Progress.Application;

public sealed class CertificateTemplateService : ICertificateTemplateService
{
    private readonly ProgressDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IInstituteBrandingReader _branding;

    public CertificateTemplateService(
        ProgressDbContext db,
        ITenantContext tenant,
        IInstituteBrandingReader branding)
    {
        _db = db;
        _tenant = tenant;
        _branding = branding;
    }

    public async Task<CertificateTemplateDto> GetAsync(CancellationToken ct = default)
    {
        var entity = await GetOrCreateEntityAsync(ct);
        return Map(entity);
    }

    public async Task<CertificateTemplateDto> SaveAsync(
        UpdateCertificateTemplateRequest request, CancellationToken ct = default)
    {
        var entity = await GetOrCreateEntityAsync(ct);
        entity.Title = request.Title.Trim();
        entity.Subtitle = request.Subtitle.Trim();
        entity.BackgroundUrl = NormalizeUrl(request.BackgroundUrl);
        entity.LogoUrl = NormalizeUrl(request.LogoUrl);
        entity.SignatureUrl = NormalizeUrl(request.SignatureUrl);
        entity.SignatureLabel = string.IsNullOrWhiteSpace(request.SignatureLabel)
            ? "Authorized signatory"
            : request.SignatureLabel.Trim();
        entity.PrimaryColor = NormalizeColor(request.PrimaryColor);
        entity.ShowQrCode = request.ShowQrCode;
        entity.Enabled = request.Enabled;
        entity.Version += 1;
        await _db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<Domain.CertificateTemplate> GetOrCreateEntityAsync(CancellationToken ct = default)
    {
        var entity = await _db.CertificateTemplates.FirstOrDefaultAsync(ct);
        if (entity is not null) return entity;

        var brand = await _branding.GetAsync(_tenant.TenantId, ct);
        entity = new CertificateTemplate
        {
            TenantId = _tenant.TenantId,
            LogoUrl = brand?.LogoUrl,
            PrimaryColor = "#0b3d91",
            Enabled = true,
            Version = 1
        };
        _db.CertificateTemplates.Add(entity);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return await _db.CertificateTemplates.FirstAsync(ct);
        }

        return entity;
    }

    private static CertificateTemplateDto Map(CertificateTemplate e) =>
        new(
            e.Title,
            e.Subtitle,
            e.BackgroundUrl,
            e.LogoUrl,
            e.SignatureUrl,
            e.SignatureLabel,
            e.PrimaryColor,
            e.ShowQrCode,
            e.Enabled,
            e.Version);

    private static string? NormalizeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        return url.Trim();
    }

    private static string NormalizeColor(string color)
    {
        var c = color?.Trim() ?? "";
        if (c.Length == 0) return "#0b3d91";
        return c.StartsWith('#') ? c : $"#{c}";
    }
}
