namespace Lms.Modules.Courses.Application;

public sealed record CreateBundleRequest(
    string Title,
    decimal Price,
    int ValidityDays,
    bool VideosOnly = false,
    int? MaxEnrollments = null,
    DateTime? EnrollmentOpensAt = null,
    DateTime? EnrollmentClosesAt = null,
    DateTime? StartsAt = null,
    DateTime? EndsAt = null);

public sealed record UpdateBundleRequest(
    decimal Price,
    int? ValidityDays = null,
    bool? VideosOnly = null,
    int? MaxEnrollments = null,
    DateTime? EnrollmentOpensAt = null,
    DateTime? EnrollmentClosesAt = null,
    DateTime? StartsAt = null,
    DateTime? EndsAt = null);

public sealed record CreateSubjectRequest(
    string Title,
    int Order,
    Guid? SubjectDefinitionId = null,
    bool IncludeSharedContent = false);

public sealed record CreateUnitRequest(string Title, int Order);

public sealed record CreateTopicRequest(string Title, int Order, bool HasVideo);

public sealed record UpdateSubjectRequest(string Title);

public sealed record UpdateUnitRequest(string Title);

public sealed record UpdateTopicRequest(string Title);
