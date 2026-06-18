using Lms.Shared.Tenancy;

namespace Lms.Modules.Courses.Application;

public sealed record LinkedBatchPlacementDto(Guid BundleId, string BundleTitle, Guid SubjectId);

public sealed record SubjectDefinitionDto(
    Guid Id,
    string Code,
    string DisplayName,
    string? Category,
    int SortOrder,
    bool IsActive,
    int LinkedBatchCount,
    int LibraryUnitCount,
    IReadOnlyList<LinkedBatchPlacementDto> LinkedBatches);

public sealed record CreateSubjectDefinitionRequest(
    string DisplayName,
    string? Code,
    ProductProfile? Category,
    int SortOrder);

public sealed record UpdateSubjectDefinitionRequest(
    string DisplayName,
    int SortOrder,
    bool IsActive,
    ProductProfile? Category);

public sealed record CreateLibraryUnitRequest(string Title, int Order);

public sealed record UpdateLibraryUnitRequest(string Title, int Order);

public sealed record LinkSharedUnitsRequest(IReadOnlyList<Guid>? UnitIds);
