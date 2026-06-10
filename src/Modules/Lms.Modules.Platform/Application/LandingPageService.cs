using System.Text.Json;
using Lms.Modules.Platform.Domain;
using Lms.Modules.Platform.Infrastructure;
using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Platform.Application;

public sealed class LandingPageService : ILandingPageService
{
    private readonly PlatformDbContext _db;
    private readonly ITenantContext _tenant;

    public LandingPageService(PlatformDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<LandingPageDto?> GetPublicAsync(string slug, CancellationToken ct = default)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        var tenant = await _db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Slug == normalized && t.Status != TenantStatus.Suspended, ct);
        if (tenant is null) return null;

        var page = await _db.LandingPages.IgnoreQueryFilters().AsNoTracking()
            .Include(p => p.Sections)
            .FirstOrDefaultAsync(p => p.TenantId == tenant.Id, ct);

        if (page is null) return new LandingPageDto(tenant.Slug, Array.Empty<PageSectionDto>());

        var sections = page.Sections
            .Where(s => s.IsEnabled)
            .OrderBy(s => s.SortOrder)
            .Select(Map)
            .ToList();

        return new LandingPageDto(tenant.Slug, sections);
    }

    public async Task<LandingPageDto> GetAdminAsync(CancellationToken ct = default)
    {
        var page = await GetOrCreatePageAsync(ct);
        var tenant = await _db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == _tenant.TenantId, ct);

        var sections = page.Sections
            .OrderBy(s => s.SortOrder)
            .Select(Map)
            .ToList();

        return new LandingPageDto(tenant?.Slug ?? "demo", sections);
    }

    public async Task<LandingPageDto> UpdateAdminAsync(
        UpdateLandingPageRequest request, CancellationToken ct = default)
    {
        ValidateSections(request.Sections);

        var page = await GetOrCreatePageAsync(ct, includeSections: false);

        await _db.PageSections
            .Where(s => s.LandingPageId == page.Id)
            .ExecuteDeleteAsync(ct);

        var order = 0;
        foreach (var dto in request.Sections.OrderBy(s => s.SortOrder))
        {
            _db.PageSections.Add(new PageSection
            {
                LandingPageId = page.Id,
                SectionType = dto.SectionType.Trim(),
                SortOrder = order++,
                ContentJson = dto.ContentJson,
                IsEnabled = dto.IsEnabled
            });
        }

        await _db.SaveChangesAsync(ct);
        return await GetAdminAsync(ct);
    }

    private async Task<LandingPage> GetOrCreatePageAsync(CancellationToken ct, bool includeSections = true)
    {
        IQueryable<LandingPage> query = _db.LandingPages;
        if (includeSections)
            query = query.Include(p => p.Sections);

        var page = await query.FirstOrDefaultAsync(ct);

        if (page is null)
        {
            page = new LandingPage { TenantId = _tenant.TenantId };
            _db.LandingPages.Add(page);
            await _db.SaveChangesAsync(ct);
        }

        return page;
    }

    private static void ValidateSections(IReadOnlyList<PageSectionDto> sections)
    {
        foreach (var s in sections)
        {
            if (!LandingSectionTypes.All.Contains(s.SectionType))
                throw new InvalidOperationException($"Unknown section type: {s.SectionType}");

            try
            {
                JsonDocument.Parse(s.ContentJson);
            }
            catch (JsonException)
            {
                throw new InvalidOperationException($"Invalid JSON for section {s.SectionType}.");
            }
        }
    }

    private static PageSectionDto Map(PageSection s) =>
        new(s.Id, s.SectionType, s.SortOrder, s.ContentJson, s.IsEnabled);
}
