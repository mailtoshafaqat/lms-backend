using Lms.Modules.Courses.Contracts;
using Lms.Modules.Enrollment.Infrastructure;
using Lms.Shared.Common;
using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;
using EnrollmentEntity = Lms.Modules.Enrollment.Domain.Enrollment;

namespace Lms.Modules.Enrollment.Application;

public sealed class EnrollmentService : IEnrollmentService
{
    private readonly EnrollmentDbContext _db;
    private readonly IBundleCatalog _catalog;
    private readonly ITenantContext _tenant;
    private readonly ITenantFeaturesProvider _features;

    public EnrollmentService(
        EnrollmentDbContext db,
        IBundleCatalog catalog,
        ITenantContext tenant,
        ITenantFeaturesProvider features)
    {
        _db = db;
        _catalog = catalog;
        _tenant = tenant;
        _features = features;
    }

    public async Task<Result<EnrollmentDto>> EnrollAsync(Guid userId, Guid bundleId, CancellationToken ct = default)
    {
        var tenantFlags = await _features.GetAsync(_tenant.TenantId, ct);
        if (tenantFlags is not null && !tenantFlags.AllowStudentSelfEnroll)
            return Result<EnrollmentDto>.Failure(
                "Self-enrollment is disabled. Contact your institute administrator.");

        return await CreateEnrollmentAsync(userId, bundleId, ct);
    }

    public Task<Result<EnrollmentDto>> ProvisionEnrollmentAsync(
        Guid userId, Guid bundleId, CancellationToken ct = default) =>
        CreateEnrollmentAsync(userId, bundleId, ct);

    private async Task<Result<EnrollmentDto>> CreateEnrollmentAsync(
        Guid userId, Guid bundleId, CancellationToken ct)
    {
        var bundle = await _catalog.GetBundleAsync(bundleId, ct);
        if (bundle is null || !bundle.IsPublished)
            return Result<EnrollmentDto>.Failure("Bundle not found.");

        var existing = await _db.Enrollments
            .FirstOrDefaultAsync(e => e.UserId == userId && e.BundleId == bundleId, ct);
        if (existing is not null)
            return Result<EnrollmentDto>.Failure("Already enrolled in this bundle.");

        var now = DateTime.UtcNow;
        var enrollment = new EnrollmentEntity
        {
            TenantId = _tenant.TenantId,
            UserId = userId,
            BundleId = bundle.Id,
            BundleTitle = bundle.Title,
            PricePaid = bundle.Price,
            EnrolledAt = now,
            ExpiresAt = now.AddDays(bundle.ValidityDays)
        };

        _db.Enrollments.Add(enrollment);
        await _db.SaveChangesAsync(ct);

        return Result<EnrollmentDto>.Success(await MapAsync(enrollment, ct));
    }

    public Task<IReadOnlyList<EnrollmentDto>> GetMyEnrollmentsAsync(
        Guid userId, CancellationToken ct = default) =>
        GetEnrollmentsForUserAsync(userId, ct);

    public async Task<IReadOnlyList<EnrollmentDto>> GetEnrollmentsForUserAsync(
        Guid userId, CancellationToken ct = default)
    {
        var rows = await _db.Enrollments
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync(ct);

        var result = new List<EnrollmentDto>(rows.Count);
        foreach (var row in rows)
            result.Add(await MapAsync(row, ct));
        return result;
    }

    public async Task<Result<EnrollmentDto>> ExtendEnrollmentAsync(
        Guid userId, Guid bundleId, DateTime expiresAt, CancellationToken ct = default)
    {
        if (expiresAt <= DateTime.UtcNow)
            return Result<EnrollmentDto>.Failure("Expiry must be in the future.");

        var enrollment = await _db.Enrollments
            .FirstOrDefaultAsync(e => e.UserId == userId && e.BundleId == bundleId, ct);
        if (enrollment is null)
            return Result<EnrollmentDto>.Failure("Enrollment not found.");

        enrollment.ExpiresAt = expiresAt;
        await _db.SaveChangesAsync(ct);

        return Result<EnrollmentDto>.Success(await MapAsync(enrollment, ct));
    }

    private async Task<EnrollmentDto> MapAsync(EnrollmentEntity e, CancellationToken ct)
    {
        var bundle = await _catalog.GetBundleAsync(e.BundleId, ct);
        return new EnrollmentDto(
            e.BundleId,
            e.BundleTitle,
            e.PricePaid,
            e.EnrolledAt,
            e.ExpiresAt,
            e.IsActive,
            bundle?.VideosOnly ?? false);
    }
}
