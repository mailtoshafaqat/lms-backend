namespace Lms.Modules.Enrollment.Application;

public sealed record EnrollmentDto(
    Guid Id,
    Guid BundleId,
    string BundleTitle,
    decimal PricePaid,
    DateTime EnrolledAt,
    DateTime ExpiresAt,
    bool IsActive,
    bool VideosOnly);

public sealed record ExtendEnrollmentRequest(DateTime ExpiresAt);
