using Lms.Modules.Courses.Domain;
using Lms.Modules.Courses.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Courses.Application;

/// <summary>Maps legacy free-text batch subjects to catalog definitions by normalized title.</summary>
public static class SubjectCatalogMigrator
{
    private static readonly Dictionary<string, string> TitleToCode = new(StringComparer.OrdinalIgnoreCase)
    {
        ["physics"] = "physics",
        ["phy"] = "physics",
        ["physic"] = "physics",
        ["chemistry"] = "chemistry",
        ["chem"] = "chemistry",
        ["biology"] = "biology",
        ["bio"] = "biology",
        ["english"] = "english",
        ["eng"] = "english",
        ["logical reasoning"] = "logical-reasoning",
        ["logical-reasoning"] = "logical-reasoning",
        ["lr"] = "logical-reasoning",
        ["reasoning"] = "logical-reasoning"
    };

    public static async Task MigrateUnlinkedSubjectsAsync(CoursesDbContext db, CancellationToken ct = default)
    {
        var unlinked = await db.Subjects.IgnoreQueryFilters()
            .Where(s => s.SubjectDefinitionId == null)
            .ToListAsync(ct);

        if (unlinked.Count == 0) return;

        var definitions = await db.SubjectDefinitions.IgnoreQueryFilters()
            .ToListAsync(ct);

        var byTenantCode = definitions
            .GroupBy(d => d.TenantId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(d => d.Code, d => d.Id, StringComparer.OrdinalIgnoreCase));

        var changed = false;
        foreach (var subject in unlinked)
        {
            var normalized = NormalizeTitle(subject.Title);
            if (!TitleToCode.TryGetValue(normalized, out var code)) continue;
            if (!byTenantCode.TryGetValue(subject.TenantId, out var codes)) continue;
            if (!codes.TryGetValue(code, out var definitionId)) continue;

            subject.SubjectDefinitionId = definitionId;
            var def = definitions.First(d => d.Id == definitionId);
            subject.Title = def.DisplayName;
            changed = true;
        }

        if (changed)
            await db.SaveChangesAsync(ct);
    }

    private static string NormalizeTitle(string title) =>
        title.Trim().ToLowerInvariant();
}
