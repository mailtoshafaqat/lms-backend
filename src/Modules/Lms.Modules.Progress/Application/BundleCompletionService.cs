using Lms.Modules.Progress.Infrastructure;
using Lms.Shared.Content;
using Lms.Shared.Courses;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Progress.Application;

/// <summary>
/// A topic is complete when the student has submitted its quiz OR watched all lectures (≥90%).
/// A bundle is complete when every topic in the bundle is complete.
/// </summary>
public sealed class BundleCompletionService
{
    public const int WatchedThresholdPercent = 90;

    private readonly ProgressDbContext _db;
    private readonly ICourseTopicCatalog _topics;
    private readonly ILectureCatalog _lectures;

    public BundleCompletionService(
        ProgressDbContext db,
        ICourseTopicCatalog topics,
        ILectureCatalog lectures)
    {
        _db = db;
        _topics = topics;
        _lectures = lectures;
    }

    public async Task<(int Completed, int Total)> GetBundleCompletionAsync(
        Guid userId, Guid bundleId, CancellationToken ct = default)
    {
        var topicIds = await GetTopicIdsForBundleAsync(bundleId, ct);
        if (topicIds.Count == 0) return (0, 0);

        var completed = 0;
        foreach (var topicId in topicIds)
        {
            if (await IsTopicCompleteAsync(userId, topicId, ct))
                completed++;
        }

        return (completed, topicIds.Count);
    }

    public async Task<bool> IsBundleCompleteAsync(
        Guid userId, Guid bundleId, CancellationToken ct = default)
    {
        var (completed, total) = await GetBundleCompletionAsync(userId, bundleId, ct);
        return total > 0 && completed == total;
    }

    public async Task<bool> IsTopicCompleteAsync(
        Guid userId, Guid topicId, CancellationToken ct = default)
    {
        var hasQuiz = await _db.QuizResults.AsNoTracking()
            .AnyAsync(r => r.UserId == userId && r.TopicId == topicId, ct);
        if (hasQuiz) return true;

        var lectureMap = await _lectures.GetLectureIdsByTopicAsync([topicId], ct);
        if (!lectureMap.TryGetValue(topicId, out var lectureIds) || lectureIds.Count == 0)
            return false;

        var watched = await _db.LectureWatchProgress.AsNoTracking()
            .Where(p => p.UserId == userId && lectureIds.Contains(p.LectureId))
            .Select(p => new { p.LectureId, p.ProgressPercent })
            .ToListAsync(ct);

        var watchedSet = watched
            .Where(p => p.ProgressPercent >= WatchedThresholdPercent)
            .Select(p => p.LectureId)
            .ToHashSet();

        return lectureIds.All(id => watchedSet.Contains(id));
    }

    private async Task<IReadOnlyList<Guid>> GetTopicIdsForBundleAsync(
        Guid bundleId, CancellationToken ct)
    {
        var paths = await _topics.GetTopicPathsForBundlesAsync([bundleId], ct);
        return paths.Select(p => p.TopicId).Distinct().ToList();
    }
}
