using Lms.Modules.Platform.Domain;
using Lms.Modules.Platform.Infrastructure;
using Lms.Shared.Common;
using Lms.Shared.Courses;
using Lms.Shared.Tenancy;
using Lms.Shared.Users;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Platform.Application;

public sealed class TenantAdminService : ITenantAdminService
{
    private readonly PlatformDbContext _db;
    private readonly IInstituteAdminProvisioner _provisioner;
    private readonly ISubjectCatalogProvisioner _catalog;

    public TenantAdminService(
        PlatformDbContext db,
        IInstituteAdminProvisioner provisioner,
        ISubjectCatalogProvisioner catalog)
    {
        _db = db;
        _provisioner = provisioner;
        _catalog = catalog;
    }

    public async Task<IReadOnlyList<TenantListItemDto>> ListAsync(CancellationToken ct = default)
    {
        return await _db.Tenants
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TenantListItemDto(
                t.Id, t.Name, t.Slug, t.Status, t.Plan, t.TrialEndsAt, t.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<TenantDetailDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var t = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return t is null ? null : MapDetail(t);
    }

    public async Task<Result<TenantDetailDto>> CreateAsync(CreateTenantRequest request, CancellationToken ct = default)
    {
        var name = request.Name.Trim();
        var slug = request.Slug.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(name))
            return Result<TenantDetailDto>.Failure("Institute name is required.");
        if (string.IsNullOrWhiteSpace(slug) || slug.Any(c => !char.IsLetterOrDigit(c) && c != '-'))
            return Result<TenantDetailDto>.Failure("Slug must be lowercase letters, numbers, or hyphens.");

        if (await _db.Tenants.AnyAsync(t => t.Slug == slug, ct))
            return Result<TenantDetailDto>.Failure("This slug is already in use.");

        var profile = request.ProductProfile;
        var seed = new TenantProfileSeed();
        ProductProfileDefaults.Apply(seed, profile);

        var tenant = new Tenant
        {
            Name = name,
            Slug = slug,
            Plan = string.IsNullOrWhiteSpace(request.Plan) ? "MVP" : request.Plan.Trim(),
            ProductProfile = profile,
            Status = TenantStatus.Trial,
            TrialEndsAt = TenantTrial.DefaultEndsAt(DateTime.UtcNow),
            ZoomMode = ZoomMode.TenantManaged,
            PaymentMode = PaymentMode.TenantManaged,
            AllowAdminCreateStudent = true,
            LiveClassesEnabled = seed.LiveClassesEnabled,
            SyllabusMentorEnabled = seed.SyllabusMentorEnabled,
            McqBulkImportEnabled = seed.McqBulkImportEnabled,
            AllowStudentSelfEnroll = seed.AllowStudentSelfEnroll
        };

        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(ct);
        await _catalog.EnsureTemplateForTenantAsync(tenant.Id, profile, ct);

        return Result<TenantDetailDto>.Success(MapDetail(tenant));
    }

    public async Task<Result<TenantDetailDto>> UpdateFlagsAsync(
        Guid id, UpdateTenantFlagsRequest request, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tenant is null)
            return Result<TenantDetailDto>.Failure("Tenant not found.");

        tenant.Status = request.Status;
        if (request.Status == TenantStatus.Active)
            tenant.TrialEndsAt = null;
        else if (request.TrialEndsAt.HasValue)
            tenant.TrialEndsAt = DateTime.SpecifyKind(request.TrialEndsAt.Value, DateTimeKind.Utc);
        else if (request.Status == TenantStatus.Trial && tenant.TrialEndsAt is null)
            tenant.TrialEndsAt = TenantTrial.DefaultEndsAt(DateTime.UtcNow);

        tenant.Plan = request.Plan.Trim();
        tenant.ProductProfile = request.ProductProfile;
        tenant.LiveClassesEnabled = request.LiveClassesEnabled;
        tenant.ZoomMode = request.ZoomMode;
        tenant.PaymentMode = request.PaymentMode;
        tenant.AllowStudentSelfEnroll = request.AllowStudentSelfEnroll;
        tenant.AllowAdminCreateStudent = request.AllowAdminCreateStudent;
        tenant.SyllabusMentorEnabled = request.SyllabusMentorEnabled;
        tenant.BundlePriceEditEnabled = request.BundlePriceEditEnabled;
        tenant.McqBulkImportEnabled = request.McqBulkImportEnabled;

        var domain = string.IsNullOrWhiteSpace(request.CustomDomain)
            ? null
            : request.CustomDomain.Trim().ToLowerInvariant();
        if (domain is not null
            && await _db.Tenants.AnyAsync(t => t.CustomDomain == domain && t.Id != id, ct))
            return Result<TenantDetailDto>.Failure("This custom domain is already in use.");
        tenant.CustomDomain = domain;

        await _db.SaveChangesAsync(ct);
        await _catalog.EnsureTemplateForTenantAsync(tenant.Id, tenant.ProductProfile, ct);
        return Result<TenantDetailDto>.Success(MapDetail(tenant));
    }

    public async Task<Result<TenantDetailDto>> ExtendTrialAsync(Guid id, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tenant is null)
            return Result<TenantDetailDto>.Failure("Tenant not found.");

        var now = DateTime.UtcNow;
        var baseDate = tenant.TrialEndsAt.HasValue && tenant.TrialEndsAt > now
            ? tenant.TrialEndsAt.Value
            : now;
        tenant.TrialEndsAt = baseDate.AddDays(TenantTrial.DefaultTrialDays);

        await _db.SaveChangesAsync(ct);
        return Result<TenantDetailDto>.Success(MapDetail(tenant));
    }

    public Task<Result<CreatedInstituteAdminDto>> CreateInstituteAdminAsync(
        Guid tenantId, CreateTenantAdminRequest request, CancellationToken ct = default) =>
        _provisioner.CreateAsync(tenantId, request.Email, request.FullName, ct);

    public Task<Result<ResetInstituteAdminPasswordDto>> ResetInstituteAdminPasswordAsync(
        Guid tenantId, Guid userId, CancellationToken ct = default) =>
        _provisioner.ResetPasswordAsync(tenantId, userId, ct);

    private static TenantDetailDto MapDetail(Tenant t) => new(
        t.Id, t.Name, t.Slug, t.CustomDomain, t.Status, t.Plan, t.ProductProfile,
        t.LiveClassesEnabled, t.ZoomMode, t.PaymentMode,
        t.AllowStudentSelfEnroll, t.AllowAdminCreateStudent, t.SyllabusMentorEnabled,
        t.BundlePriceEditEnabled, t.McqBulkImportEnabled, t.TrialEndsAt, t.CreatedAt);
}
