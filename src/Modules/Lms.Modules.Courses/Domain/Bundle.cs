using Lms.Shared.Entities;

namespace Lms.Modules.Courses.Domain;

/// <summary>A sellable program/batch, e.g. "MDCAT Premium 2026". Top of the content tree.</summary>
public sealed class Bundle : TenantEntity
{
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int ValidityDays { get; set; } = 365;
    public bool IsPublished { get; set; } = true;

    public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
}
