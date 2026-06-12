using Lms.Modules.Content.Infrastructure;
using Lms.Modules.Courses.Contracts;
using Lms.Shared.Auth;
using Lms.Shared.Courses;
using Lms.Shared.Enrollments;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Content.Application;

public interface IVideoLibraryService
{
    Task<VideoLibraryDto> GetMyLibraryAsync(CancellationToken ct = default);
}

public sealed class VideoLibraryService : IVideoLibraryService
{
    private readonly ContentDbContext _db;
    private readonly IEnrollmentReader _enrollments;
    private readonly ICourseTopicCatalog _topics;
    private readonly IBundleCatalog _bundles;
    private readonly ICurrentUser _user;

    public VideoLibraryService(
        ContentDbContext db,
        IEnrollmentReader enrollments,
        ICourseTopicCatalog topics,
        IBundleCatalog bundles,
        ICurrentUser user)
    {
        _db = db;
        _enrollments = enrollments;
        _topics = topics;
        _bundles = bundles;
        _user = user;
    }

    public async Task<VideoLibraryDto> GetMyLibraryAsync(CancellationToken ct = default)
    {
        var userId = _user.UserId;
        if (userId is null)
            return new VideoLibraryDto(false, []);

        var bundleIds = await _enrollments.GetActiveBundleIdsAsync(userId.Value, ct);
        if (bundleIds.Count == 0)
            return new VideoLibraryDto(false, []);

        var videosOnlyStudent = true;
        foreach (var id in bundleIds)
        {
            var bundle = await _bundles.GetBundleAsync(id, ct);
            if (bundle is null || !bundle.VideosOnly)
            {
                videosOnlyStudent = false;
                break;
            }
        }

        var topicPaths = await _topics.GetTopicPathsForBundlesAsync(bundleIds, ct);
        if (topicPaths.Count == 0)
            return new VideoLibraryDto(videosOnlyStudent, []);

        var topicIds = topicPaths.Select(p => p.TopicId).Distinct().ToList();
        var pathByTopic = topicPaths.ToDictionary(p => p.TopicId);

        var lectureRows = await _db.Lectures
            .AsNoTracking()
            .Where(l => topicIds.Contains(l.TopicId))
            .OrderBy(l => l.Order)
            .ToListAsync(ct);

        var items = new List<VideoLibraryItemDto>();
        foreach (var lecture in lectureRows)
        {
            if (!pathByTopic.TryGetValue(lecture.TopicId, out var path)) continue;

            var locked = lecture.MembersOnly && userId is null;
            string? playUrl = locked
                ? null
                : lecture.Url ?? (lecture.StorageKey != null ? $"/api/v1/files/{lecture.StorageKey}" : null);
            if (playUrl is null) continue;

            items.Add(new VideoLibraryItemDto(
                lecture.Id,
                lecture.Title,
                playUrl,
                lecture.DurationSec,
                path.TopicId,
                path.TopicTitle,
                path.SubjectTitle,
                path.BundleTitle,
                path.BundleId));
        }

        return new VideoLibraryDto(videosOnlyStudent, items);
    }
}
