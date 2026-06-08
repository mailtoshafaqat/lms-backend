using Lms.Modules.Courses.Domain;
using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Courses.Infrastructure;

/// <summary>Seeds sample content for the default tenant (dev only) so the dashboard
/// shows real data. Idempotent: skips if any bundle already exists.</summary>
public static class CourseSeeder
{
    public static async Task SeedAsync(CoursesDbContext db, CancellationToken ct = default)
    {
        if (await db.Bundles.IgnoreQueryFilters().AnyAsync(ct)) return;

        var tenantId = TenantContext.DefaultTenantId;

        var mdcat = new Bundle { TenantId = tenantId, Title = "MDCAT Premium 2026", Price = 25000, IsPublished = true };
        var ecat = new Bundle { TenantId = tenantId, Title = "ECAT Crash Course", Price = 15000, IsPublished = true };

        var bio = new Subject { TenantId = tenantId, Title = "Biology", Order = 1 };
        var phy = new Subject { TenantId = tenantId, Title = "Physics", Order = 2 };
        var chem = new Subject { TenantId = tenantId, Title = "Chemistry", Order = 1 };
        mdcat.Subjects.Add(bio);
        mdcat.Subjects.Add(phy);
        ecat.Subjects.Add(chem);

        var cellUnit = new Unit { TenantId = tenantId, Title = "Cell Biology", Order = 1 };
        var motionUnit = new Unit { TenantId = tenantId, Title = "Mechanics", Order = 1 };
        var periodicUnit = new Unit { TenantId = tenantId, Title = "Periodic Table", Order = 1 };
        bio.Units.Add(cellUnit);
        phy.Units.Add(motionUnit);
        chem.Units.Add(periodicUnit);

        cellUnit.Topics.Add(new Topic { TenantId = tenantId, Title = "Cell Structure & Function", Order = 1, HasVideo = true, McqCount = 20, FlashcardCount = 12 });
        motionUnit.Topics.Add(new Topic { TenantId = tenantId, Title = "Newton's Laws of Motion", Order = 1, HasVideo = true, McqCount = 25, FlashcardCount = 8 });
        periodicUnit.Topics.Add(new Topic { TenantId = tenantId, Title = "Periodic Table Trends", Order = 1, HasVideo = true, McqCount = 18, FlashcardCount = 15 });

        db.Bundles.AddRange(mdcat, ecat);
        await db.SaveChangesAsync(ct);
    }
}
