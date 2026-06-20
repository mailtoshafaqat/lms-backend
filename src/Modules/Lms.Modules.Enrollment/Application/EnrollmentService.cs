using Lms.Modules.Courses.Contracts;
using Lms.Modules.Enrollment.Infrastructure;
using Lms.Shared.Common;
using Lms.Shared.Enrollments;
using Lms.Shared.Payments;
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
    private readonly ITenantPaymentSettingsProvider _payments;
    private readonly IBundleEnrollmentPolicy _batchPolicy;

    public EnrollmentService(
        EnrollmentDbContext db,
        IBundleCatalog catalog,
        ITenantContext tenant,
        ITenantFeaturesProvider features,
        ITenantPaymentSettingsProvider payments,
        IBundleEnrollmentPolicy batchPolicy)
    {
        _db = db;
        _catalog = catalog;
        _tenant = tenant;
        _features = features;
        _payments = payments;
        _batchPolicy = batchPolicy;
    }

    public async Task<Result<EnrollmentDto>> EnrollAsync(Guid userId, Guid bundleId, CancellationToken ct = default)
    {
        var tenantFlags = await _features.GetAsync(_tenant.TenantId, ct);
        if (tenantFlags is not null && !tenantFlags.AllowStudentSelfEnroll)
            return Result<EnrollmentDto>.Failure(
                "Self-enrollment is disabled. Contact your institute administrator.");

        var bundle = await _catalog.GetBundleAsync(bundleId, ct);
        if (bundle is null || !bundle.IsPublished)
            return Result<EnrollmentDto>.Failure("Bundle not found.");

        var paymentSettings = await _payments.GetAsync(ct);
        var modes = paymentSettings?.EnrollmentModes ?? EnrollmentModes.AdminOnly;

        if (bundle.Price > 0)
        {
            var requiresPayment = modes.HasFlag(EnrollmentModes.ManualPayment)
                || modes.HasFlag(EnrollmentModes.OnlineCheckout);
            if (requiresPayment)
                return Result<EnrollmentDto>.Failure(
                    "This course requires payment. Use checkout to complete your enrollment.");
        }
        else if (!modes.HasFlag(EnrollmentModes.SelfEnrollFree))
        {
            return Result<EnrollmentDto>.Failure(
                "Free self-enrollment is not enabled. Contact your institute administrator.");
        }

        return await CreateEnrollmentAsync(userId, bundleId, BundleEnrollmentCheckMode.Student, ct);
    }

    public Task<Result<EnrollmentDto>> ProvisionEnrollmentAsync(
        Guid userId, Guid bundleId, CancellationToken ct = default) =>
        CreateEnrollmentAsync(userId, bundleId, BundleEnrollmentCheckMode.AdminOverride, ct);

    private async Task<Result<EnrollmentDto>> CreateEnrollmentAsync(
        Guid userId, Guid bundleId, BundleEnrollmentCheckMode mode, CancellationToken ct)
    {
        var bundle = await _catalog.GetBundleAsync(bundleId, ct);
        if (bundle is null || !bundle.IsPublished)
            return Result<EnrollmentDto>.Failure("Bundle not found.");

        var policy = await _batchPolicy.CheckCanEnrollAsync(bundleId, mode, ct);
        if (!policy.Allowed)
            return Result<EnrollmentDto>.Failure(policy.ErrorMessage ?? "Cannot enroll in this batch.");

        var existing = await _db.Enrollments
            .FirstOrDefaultAsync(e => e.UserId == userId && e.BundleId == bundleId, ct);
        if (existing is not null)
            return Result<EnrollmentDto>.Failure("Already enrolled in this bundle.");

        var now = DateTime.UtcNow;
        var expiresAt = now.AddDays(bundle.ValidityDays);
        if (bundle.EndsAt is not null && bundle.EndsAt.Value < expiresAt)
            expiresAt = bundle.EndsAt.Value;

        var enrollment = new EnrollmentEntity
        {
            TenantId = _tenant.TenantId,
            UserId = userId,
            BundleId = bundle.Id,
            BundleTitle = bundle.Title,
            PricePaid = bundle.Price,
            EnrolledAt = now,
            ExpiresAt = expiresAt
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
            e.Id,
            e.BundleId,
            e.BundleTitle,
            e.PricePaid,
            e.EnrolledAt,
            e.ExpiresAt,
            e.IsActive,
            bundle?.VideosOnly ?? false);
    }
}
