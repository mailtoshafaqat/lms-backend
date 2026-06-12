using Lms.Modules.Courses.Domain;
using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Courses.Application;

/// <summary>Seeds profile-specific subject catalog templates per tenant.</summary>
public static class SubjectCatalogSeeder
{
    private static readonly (string Code, string DisplayName, int SortOrder)[] ExamPrepTemplate =
    [
        ("physics", "Physics", 1),
        ("chemistry", "Chemistry", 2),
        ("biology", "Biology", 3),
        ("english", "English", 4),
        ("logical-reasoning", "Logical Reasoning", 5)
    ];

    private static readonly (string Code, string DisplayName, int SortOrder)[] GeneralLmsTemplate =
    [
        ("module-1", "Module 1", 1),
        ("module-2", "Module 2", 2),
        ("module-3", "Module 3", 3),
        ("module-4", "Module 4", 4),
        ("module-5", "Module 5", 5)
    ];

    public static async Task SeedForTenantAsync(
        Infrastructure.CoursesDbContext db,
        Guid tenantId,
        ProductProfile profile,
        CancellationToken ct = default)
    {
        if (await db.SubjectDefinitions.IgnoreQueryFilters()
                .AnyAsync(d => d.TenantId == tenantId, ct))
            return;

        var template = TemplateForProfile(profile);
        if (template.Length == 0)
            return;

        foreach (var (code, displayName, sortOrder) in template)
        {
            db.SubjectDefinitions.Add(new SubjectDefinition
            {
                TenantId = tenantId,
                Code = code,
                DisplayName = displayName,
                Category = profile,
                SortOrder = sortOrder,
                IsActive = true
            });
        }

        await db.SaveChangesAsync(ct);
    }

    private static (string Code, string DisplayName, int SortOrder)[] TemplateForProfile(
        ProductProfile profile) =>
        profile switch
        {
            ProductProfile.ExamPrep => ExamPrepTemplate,
            ProductProfile.GeneralLms => GeneralLmsTemplate,
            ProductProfile.Both => [.. ExamPrepTemplate, .. GeneralLmsTemplate],
            _ => []
        };

    public static async Task EnsureDefaultTenantAsync(
        Infrastructure.CoursesDbContext db,
        CancellationToken ct = default) =>
        await SeedForTenantAsync(db, TenantContext.DefaultTenantId, ProductProfile.ExamPrep, ct);
}
