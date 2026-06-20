using Lms.Modules.Courses.Domain;
using Lms.Shared.Enrollments;

namespace Lms.Modules.Courses.Application;

public interface IBundleDtoMapper
{
    Task<BundleDto> MapAsync(Bundle bundle, CancellationToken ct = default);
}

public sealed class BundleDtoMapper : IBundleDtoMapper
{
    private readonly IEnrollmentReader _enrollments;

    public BundleDtoMapper(IEnrollmentReader enrollments) => _enrollments = enrollments;

    public async Task<BundleDto> MapAsync(Bundle bundle, CancellationToken ct = default)
    {
        var active = await _enrollments.CountActiveEnrollmentsAsync(bundle.Id, ct);
        var status = BundleEnrollmentStatus.Compute(bundle, active, DateTime.UtcNow);
        return new BundleDto(
            bundle.Id,
            bundle.Title,
            bundle.Subjects.Count,
            bundle.Price,
            bundle.VideosOnly,
            bundle.ValidityDays,
            bundle.MaxEnrollments,
            active,
            bundle.EnrollmentOpensAt,
            bundle.EnrollmentClosesAt,
            bundle.StartsAt,
            bundle.EndsAt,
            status);
    }
}
