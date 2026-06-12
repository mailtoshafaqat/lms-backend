using Lms.Shared.Tenancy;

namespace Lms.Modules.Platform.Domain;

/// <summary>An institute (customer) on the SaaS platform. SuperAdmin creates and configures flags;
/// the tenant's InstituteAdmin runs day-to-day LMS operations (BYO Zoom, BYO payments).</summary>
public sealed class Tenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;

    /// <summary>Optional custom apex domain (e.g. academy.com). Resolved before subdomain slug.</summary>
    public string? CustomDomain { get; set; }
    public TenantStatus Status { get; set; } = TenantStatus.Trial;
    public string Plan { get; set; } = "MVP";

    public ProductProfile ProductProfile { get; set; } = ProductProfile.ExamPrep;

    public bool LiveClassesEnabled { get; set; } = true;
    public ZoomMode ZoomMode { get; set; } = ZoomMode.TenantManaged;
    public PaymentMode PaymentMode { get; set; } = PaymentMode.TenantManaged;
    public bool AllowStudentSelfEnroll { get; set; }
    public bool AllowAdminCreateStudent { get; set; } = true;
    public bool SyllabusMentorEnabled { get; set; } = true;

    /// <summary>InstituteAdmin can edit bundle list prices (commercial catalog).</summary>
    public bool BundlePriceEditEnabled { get; set; } = true;

    /// <summary>Teachers and admins can bulk-import MCQs via CSV on topic quiz tab.</summary>
    public bool McqBulkImportEnabled { get; set; } = true;

    /// <summary>UTC end of trial period. Enforced when <see cref="Status"/> is Trial.</summary>
    public DateTime? TrialEndsAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
