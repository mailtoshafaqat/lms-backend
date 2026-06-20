using Lms.Modules.Courses.Domain;

namespace Lms.Modules.Courses.Application;

internal static class BundleEnrollmentStatus
{
    public static string Compute(
        Bundle bundle, int activeEnrollments, DateTime utcNow)
    {
        if (bundle.EndsAt is not null && utcNow > bundle.EndsAt.Value)
            return "Ended";
        if (bundle.EnrollmentOpensAt is not null && utcNow < bundle.EnrollmentOpensAt.Value)
            return "NotYetOpen";
        if (bundle.EnrollmentClosesAt is not null && utcNow > bundle.EnrollmentClosesAt.Value)
            return "Closed";
        if (bundle.MaxEnrollments is > 0 && activeEnrollments >= bundle.MaxEnrollments.Value)
            return "Full";
        return "Open";
    }
}
