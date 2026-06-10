namespace Lms.Modules.Courses.Application;

public sealed record CreateBundleRequest(string Title, decimal Price, int ValidityDays);

public sealed record UpdateBundleRequest(decimal Price, int? ValidityDays = null);

public sealed record CreateSubjectRequest(string Title, int Order);

public sealed record CreateUnitRequest(string Title, int Order);

public sealed record CreateTopicRequest(string Title, int Order, bool HasVideo);
