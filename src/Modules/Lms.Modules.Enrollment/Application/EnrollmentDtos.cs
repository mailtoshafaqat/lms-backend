namespace Lms.Modules.Enrollment.Application;

public sealed record EnrollmentDto(
    Guid BundleId,
    string BundleTitle,
    decimal PricePaid,
    DateTime EnrolledAt,
    DateTime ExpiresAt,
    bool IsActive);

public sealed record ExtendEnrollmentRequest(DateTime ExpiresAt);
