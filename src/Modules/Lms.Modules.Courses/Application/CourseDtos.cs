namespace Lms.Modules.Courses.Application;

public sealed record BundleDto(Guid Id, string Title, int SubjectCount, decimal Price, bool VideosOnly);

public sealed record BundleDetailDto(Guid Id, string Title, IReadOnlyList<SubjectDto> Subjects);

public sealed record SubjectDto(
    Guid Id,
    string Title,
    int Order,
    int UnitCount,
    Guid? SubjectDefinitionId = null,
    bool LinkedToCatalog = false,
    int SharedUnitLinkCount = 0);

public sealed record UnitDto(
    Guid Id,
    string Title,
    int Order,
    int TopicCount,
    bool IsShared = false);

public sealed record TopicDto(
    Guid Id,
    string Title,
    int Order,
    bool HasVideo,
    int McqCount,
    int FlashcardCount);
