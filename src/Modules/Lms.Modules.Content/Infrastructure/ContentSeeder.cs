using Lms.Modules.Content.Domain;
using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Content.Infrastructure;

/// <summary>Seeds a sample lecture + note per topic (dev only). Topics are passed in by the
/// host so the Content module stays decoupled from the Courses module.</summary>
public static class ContentSeeder
{
    private const string SampleVideoUrl =
        "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4";

    public static async Task SeedAsync(
        ContentDbContext db,
        IEnumerable<(Guid TopicId, string Title)> topics,
        CancellationToken ct = default)
    {
        if (await db.Lectures.IgnoreQueryFilters().AnyAsync(ct)) return;

        var tenantId = TenantContext.DefaultTenantId;

        foreach (var (topicId, title) in topics)
        {
            db.Lectures.Add(new Lecture
            {
                TenantId = tenantId,
                TopicId = topicId,
                Title = $"{title} — Lecture",
                Url = SampleVideoUrl,
                DurationSec = 600,
                Order = 1
            });

            db.Notes.Add(new Note
            {
                TenantId = tenantId,
                TopicId = topicId,
                Title = $"{title} — Notes",
                ContentHtml = $"<h2>{title}</h2><p>Key concepts and summary for {title}.</p>",
                Order = 1
            });
        }

        await db.SaveChangesAsync(ct);
    }
}
