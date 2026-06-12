using Lms.Modules.Courses.Infrastructure;
using Lms.Shared.Courses;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Courses.Application;

public sealed class TopicFlashcardStats : ITopicFlashcardStats
{
    private readonly CoursesDbContext _db;

    public TopicFlashcardStats(CoursesDbContext db) => _db = db;

    public async Task SetFlashcardCountAsync(Guid topicId, int count, CancellationToken ct = default)
    {
        var topic = await _db.Topics.FirstOrDefaultAsync(t => t.Id == topicId, ct);
        if (topic is null) return;

        topic.FlashcardCount = Math.Max(0, count);
        await _db.SaveChangesAsync(ct);
    }
}
