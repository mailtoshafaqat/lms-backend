using System.Text.Json;
using Lms.Modules.Platform.Application;
using Lms.Modules.Platform.Domain;
using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Platform.Infrastructure;

public static class PlatformSeeder
{
    public static async Task SeedAsync(PlatformDbContext db, CancellationToken ct = default)
    {
        if (!await db.Tenants.AnyAsync(t => t.Id == TenantContext.DefaultTenantId, ct))
        {
            db.Tenants.Add(new Tenant
            {
                Id = TenantContext.DefaultTenantId,
                Name = "Demo Academy",
                Slug = "demo",
                Status = TenantStatus.Active,
                Plan = "MVP",
                LiveClassesEnabled = true,
                ZoomMode = ZoomMode.TenantManaged,
                PaymentMode = PaymentMode.TenantManaged,
                AllowStudentSelfEnroll = false,
                AllowAdminCreateStudent = true
            });
            await db.SaveChangesAsync(ct);
        }

        if (!await db.TenantSettings.IgnoreQueryFilters()
                .AnyAsync(s => s.TenantId == TenantContext.DefaultTenantId, ct))
        {
            db.TenantSettings.Add(new TenantSettings
            {
                TenantId = TenantContext.DefaultTenantId,
                DisplayName = "Demo Academy",
                PrimaryColor = "#0b3d91"
            });
            await db.SaveChangesAsync(ct);
        }

        if (!await db.LandingPages.IgnoreQueryFilters()
                .AnyAsync(p => p.TenantId == TenantContext.DefaultTenantId, ct))
        {
            var page = new LandingPage { TenantId = TenantContext.DefaultTenantId };
            page.Sections =
            [
                new PageSection
                {
                    SectionType = LandingSectionTypes.Hero,
                    SortOrder = 0,
                    ContentJson = JsonSerializer.Serialize(new
                    {
                        title = "Ace MDCAT & ECAT with Demo Academy",
                        subtitle = "Courses, live classes, daily practice tests, flashcards and an AI tutor — all under your brand.",
                        ctaLabel = "Log in to your account",
                        ctaHref = "/login"
                    })
                },
                new PageSection
                {
                    SectionType = LandingSectionTypes.Features,
                    SortOrder = 1,
                    ContentJson = JsonSerializer.Serialize(new
                    {
                        cards = new[]
                        {
                            new { title = "Live & recorded classes", description = "Zoom live sessions plus on-demand lectures.", icon = "Video" },
                            new { title = "MCQs & flashcards", description = "Daily practice tests with explanations.", icon = "BookOpen" },
                            new { title = "Syllabus Mentor", description = "Syllabus-scoped answers with citations — no open web.", icon = "Brain" }
                        }
                    })
                },
                new PageSection
                {
                    SectionType = LandingSectionTypes.Footer,
                    SortOrder = 2,
                    ContentJson = JsonSerializer.Serialize(new
                    {
                        text = "© Demo Academy. All rights reserved."
                    })
                }
            ];
            db.LandingPages.Add(page);
            await db.SaveChangesAsync(ct);
        }
    }
}
