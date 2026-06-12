using Lms.Modules.Progress.Domain;
using Lms.Modules.Progress.Infrastructure;
using Lms.Shared.Branding;
using Lms.Shared.Common;
using Lms.Shared.Courses;
using Lms.Shared.Tenancy;
using Lms.Shared.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Lms.Modules.Progress.Application;

public sealed class CertificateService : ICertificateService
{
    private readonly ProgressDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly BundleCompletionService _completion;
    private readonly ICourseTopicCatalog _topics;
    private readonly IUserDirectory _users;
    private readonly ICertificateTemplateService _templates;
    private readonly ICertificatePdfService _pdf;
    private readonly IInstituteBrandingReader _branding;
    private readonly ITenantFeaturesProvider _tenantFeatures;
    private readonly ITenantResolver _tenantResolver;
    private readonly string _frontendBaseUrl;

    public CertificateService(
        ProgressDbContext db,
        ITenantContext tenant,
        BundleCompletionService completion,
        ICourseTopicCatalog topics,
        IUserDirectory users,
        ICertificateTemplateService templates,
        ICertificatePdfService pdf,
        IInstituteBrandingReader branding,
        ITenantFeaturesProvider tenantFeatures,
        ITenantResolver tenantResolver,
        IConfiguration configuration)
    {
        _db = db;
        _tenant = tenant;
        _completion = completion;
        _topics = topics;
        _users = users;
        _templates = templates;
        _pdf = pdf;
        _branding = branding;
        _tenantFeatures = tenantFeatures;
        _tenantResolver = tenantResolver;
        _frontendBaseUrl = (configuration["App:BaseUrl"] ?? "http://localhost:3000").TrimEnd('/');
    }

    public async Task TryIssueIfCompleteAsync(Guid userId, Guid bundleId, CancellationToken ct = default)
    {
        var template = await _templates.GetOrCreateEntityAsync(ct);
        if (!template.Enabled) return;

        var exists = await _db.CompletionCertificates.AsNoTracking()
            .AnyAsync(c => c.UserId == userId && c.BundleId == bundleId, ct);
        if (exists) return;

        if (!await _completion.IsBundleCompleteAsync(userId, bundleId, ct))
            return;

        var bundleTitle = await ResolveBundleTitleAsync(bundleId, ct);
        var number = await GenerateCertificateNumberAsync(ct);
        var names = await _users.GetDisplayNamesAsync([userId], ct);
        var studentName = names.TryGetValue(userId, out var n) ? n : "Student";
        var brand = await _branding.GetAsync(_tenant.TenantId, ct);
        var instituteName = brand?.DisplayName ?? "Institute";

        _db.CompletionCertificates.Add(new CompletionCertificate
        {
            TenantId = _tenant.TenantId,
            UserId = userId,
            BundleId = bundleId,
            BundleTitle = bundleTitle,
            CertificateNumber = number,
            IssuedAt = DateTime.UtcNow,
            StudentName = studentName,
            InstituteName = instituteName,
            TemplateVersion = template.Version
        });

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            /* race: another request issued first */
        }
    }

    public async Task<IReadOnlyList<CertificateDto>> ListMineAsync(
        Guid userId, CancellationToken ct = default)
    {
        var slug = await ResolveCurrentSlugAsync(ct);
        var rows = await _db.CompletionCertificates.AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.IssuedAt)
            .ToListAsync(ct);

        return rows.Select(c => MapStudentDto(c, slug)).ToList();
    }

    public async Task<PagedResult<AdminCertificateDto>> ListAdminAsync(
        Guid? bundleId, int page, int pageSize, CancellationToken ct = default)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var query = _db.CompletionCertificates.AsNoTracking().AsQueryable();
        if (bundleId is Guid bid)
            query = query.Where(c => c.BundleId == bid);

        var total = await query.CountAsync(ct);
        var rows = await query
            .OrderByDescending(c => c.IssuedAt)
            .Skip((normalizedPage - 1) * normalizedSize)
            .Take(normalizedSize)
            .ToListAsync(ct);

        var data = rows.Select(r => new AdminCertificateDto(
            r.Id,
            r.UserId,
            string.IsNullOrWhiteSpace(r.StudentName) ? "Unknown" : r.StudentName,
            r.BundleId,
            r.BundleTitle,
            r.CertificateNumber,
            r.IssuedAt)).ToList();

        return new PagedResult<AdminCertificateDto>(data, normalizedPage, normalizedSize, total);
    }

    public async Task<byte[]?> GetPdfForStudentAsync(
        Guid certificateId, Guid userId, CancellationToken ct = default)
    {
        var cert = await _db.CompletionCertificates.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == certificateId && c.UserId == userId, ct);
        return cert is null ? null : await RenderPdfAsync(cert, ct);
    }

    public async Task<byte[]?> GetPdfForAdminAsync(Guid certificateId, CancellationToken ct = default)
    {
        var cert = await _db.CompletionCertificates.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == certificateId, ct);
        return cert is null ? null : await RenderPdfAsync(cert, ct);
    }

    public async Task<CertificateVerifyDto?> VerifyAsync(
        string certificateNumber, string tenantSlug, CancellationToken ct = default)
    {
        var slug = tenantSlug.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(slug) || string.IsNullOrWhiteSpace(certificateNumber))
            return new CertificateVerifyDto(false, certificateNumber, "", "", "", default, slug);

        var tenantId = await _tenantResolver.ResolveTenantIdBySlugAsync(slug, ct);
        if (tenantId is null)
            return new CertificateVerifyDto(false, certificateNumber, "", "", "", default, slug);

        var cert = await _db.CompletionCertificates.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.TenantId == tenantId && c.CertificateNumber == certificateNumber.Trim(), ct);

        if (cert is null)
            return new CertificateVerifyDto(false, certificateNumber, "", "", "", default, slug);

        return new CertificateVerifyDto(
            true,
            cert.CertificateNumber,
            cert.StudentName,
            cert.BundleTitle,
            cert.InstituteName,
            cert.IssuedAt,
            slug);
    }

    private async Task<byte[]?> RenderPdfAsync(CompletionCertificate cert, CancellationToken ct)
    {
        var template = await _templates.GetOrCreateEntityAsync(ct);
        var slug = await ResolveCurrentSlugAsync(ct) ?? "demo";
        return await _pdf.RenderAsync(cert, template, slug, ct);
    }

    private CertificateDto MapStudentDto(CompletionCertificate c, string? slug)
    {
        var verifyUrl = slug is null
            ? null
            : $"{_frontendBaseUrl}/verify/{Uri.EscapeDataString(c.CertificateNumber)}?tenant={Uri.EscapeDataString(slug)}";
        return new CertificateDto(
            c.Id, c.BundleId, c.BundleTitle, c.CertificateNumber, c.IssuedAt, verifyUrl);
    }

    private async Task<string?> ResolveCurrentSlugAsync(CancellationToken ct)
    {
        var features = await _tenantFeatures.GetAsync(_tenant.TenantId, ct);
        return features?.Slug;
    }

    private async Task<string> ResolveBundleTitleAsync(Guid bundleId, CancellationToken ct)
    {
        var paths = await _topics.GetTopicPathsForBundlesAsync([bundleId], ct);
        return paths.FirstOrDefault()?.BundleTitle ?? "Course";
    }

    private async Task<string> GenerateCertificateNumberAsync(CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
            var number = $"CERT-{year}-{suffix}";
            var taken = await _db.CompletionCertificates.IgnoreQueryFilters()
                .AnyAsync(c => c.TenantId == _tenant.TenantId && c.CertificateNumber == number, ct);
            if (!taken) return number;
        }

        return $"CERT-{year}-{Guid.NewGuid():N}"[..20];
    }
}
