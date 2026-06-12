using Lms.Shared.Entities;
using Lms.Shared.Tenancy;

namespace Lms.Modules.Courses.Domain;

/// <summary>Tenant-level catalog entry for a subject (e.g. Physics). Batch subjects link here.</summary>
public sealed class SubjectDefinition : TenantEntity
{
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public ProductProfile? Category { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Subject> BatchSubjects { get; set; } = new List<Subject>();
    public ICollection<Unit> LibraryUnits { get; set; } = new List<Unit>();
    public ICollection<SubjectDefinitionTeacher> Teachers { get; set; } = new List<SubjectDefinitionTeacher>();
}
