using Lms.Shared.Entities;

namespace Lms.Modules.Courses.Domain;

/// <summary>A sellable program/batch, e.g. "MDCAT Premium 2026". Top of the content tree.</summary>
public sealed class Bundle : TenantEntity
{
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int ValidityDays { get; set; } = 365;
    public bool IsPublished { get; set; } = true;

    /// <summary>When true, students enrolled in this bundle get video-library access only (no quizzes/live UI).</summary>
    public bool VideosOnly { get; set; }

    /// <summary>Max active students; null = unlimited.</summary>
    public int? MaxEnrollments { get; set; }

    public DateTime? EnrollmentOpensAt { get; set; }
    public DateTime? EnrollmentClosesAt { get; set; }

    /// <summary>When set, student content access begins at this UTC time (enrollment may happen earlier).</summary>
    public DateTime? StartsAt { get; set; }

    /// <summary>When set, caps enrollment expiry and blocks new enrollments after this UTC time.</summary>
    public DateTime? EndsAt { get; set; }

    public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
}
