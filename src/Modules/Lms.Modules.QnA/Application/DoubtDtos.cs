using Lms.Modules.QnA.Domain;

namespace Lms.Modules.QnA.Application;

public sealed record DoubtThreadSummaryDto(
    Guid Id,
    Guid SubjectId,
    string SubjectTitle,
    string BundleTitle,
    Guid? TopicId,
    string? TopicTitle,
    string Title,
    string Status,
    string? StudentName,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record DoubtMessageDto(
    Guid Id,
    Guid AuthorUserId,
    string AuthorName,
    string AuthorRole,
    string Body,
    DateTime CreatedAt);

public sealed record DoubtThreadDetailDto(
    Guid Id,
    Guid SubjectId,
    string SubjectTitle,
    string BundleTitle,
    Guid? TopicId,
    string? TopicTitle,
    string Title,
    string Status,
    string StudentName,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? ResolvedAt,
    IReadOnlyList<DoubtMessageDto> Messages);

public sealed record CreateDoubtRequest(Guid SubjectId, Guid? TopicId, string Question);

public sealed record AddDoubtMessageRequest(string Body);

public static class DoubtErrors
{
    public const string NotFound = "not_found";
    public const string Forbidden = "forbidden";
}
