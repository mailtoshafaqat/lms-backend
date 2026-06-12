using System.Globalization;
using System.Text;
using Lms.Modules.Progress.Infrastructure;
using Lms.Shared.Common;
using Lms.Shared.Content;
using Lms.Shared.Courses;
using Lms.Shared.Enrollments;
using Lms.Shared.Users;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Progress.Application;

public sealed class CohortAnalyticsService : ICohortAnalyticsService
{
    private readonly ProgressDbContext _db;
    private readonly IEnrollmentReader _enrollments;
    private readonly ICourseTopicCatalog _topics;
    private readonly ILectureCatalog _lectures;
    private readonly IUserDirectory _users;

    public CohortAnalyticsService(
        ProgressDbContext db,
        IEnrollmentReader enrollments,
        ICourseTopicCatalog topics,
        ILectureCatalog lectures,
        IUserDirectory users)
    {
        _db = db;
        _enrollments = enrollments;
        _topics = topics;
        _lectures = lectures;
        _users = users;
    }

    public async Task<Result<CohortAnalyticsOverviewDto>> GetOverviewAsync(
        Guid bundleId, Guid? subjectId, DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default)
    {
        var rows = await BuildStudentRowsAsync(bundleId, subjectId, fromUtc, toUtc, ct);
        if (!rows.Succeeded) return Result<CohortAnalyticsOverviewDto>.Failure(rows.Error!);

        var data = rows.Value!;
        var certCount = await _db.CompletionCertificates.AsNoTracking()
            .CountAsync(c => c.BundleId == bundleId, ct);

        return Result<CohortAnalyticsOverviewDto>.Success(new CohortAnalyticsOverviewDto(
            bundleId,
            data.BundleTitle,
            data.Students.Count,
            data.Students.Count == 0 ? 0 : (int)Math.Round(data.Students.Average(s => s.CompletionPercent)),
            data.Students.Count == 0 ? 0 : (int)Math.Round(data.Students.Average(s => s.AvgQuizAccuracy)),
            certCount));
    }

    public async Task<Result<IReadOnlyList<CohortStudentRowDto>>> GetStudentRowsAsync(
        Guid bundleId, Guid? subjectId, DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default)
    {
        var built = await BuildStudentRowsAsync(bundleId, subjectId, fromUtc, toUtc, ct);
        return built.Succeeded
            ? Result<IReadOnlyList<CohortStudentRowDto>>.Success(built.Value!.Students)
            : Result<IReadOnlyList<CohortStudentRowDto>>.Failure(built.Error!);
    }

    public async Task<Result<byte[]>> ExportCsvAsync(
        Guid bundleId, Guid? subjectId, DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default)
    {
        var built = await BuildStudentRowsAsync(bundleId, subjectId, fromUtc, toUtc, ct);
        if (!built.Succeeded) return Result<byte[]>.Failure(built.Error!);

        var sb = new StringBuilder();
        sb.AppendLine("StudentName,TopicsCompleted,TopicsTotal,CompletionPercent,AvgQuizAccuracy,VideosWatched,VideosTotal,LastActiveUtc");
        foreach (var row in built.Value!.Students)
        {
            sb.Append(Csv(row.StudentName)).Append(',');
            sb.Append(row.TopicsCompleted).Append(',');
            sb.Append(row.TopicsTotal).Append(',');
            sb.Append(row.CompletionPercent).Append(',');
            sb.Append(row.AvgQuizAccuracy).Append(',');
            sb.Append(row.VideosWatched).Append(',');
            sb.Append(row.VideosTotal).Append(',');
            sb.Append(row.LastActiveAt?.ToString("o", CultureInfo.InvariantCulture) ?? "");
            sb.AppendLine();
        }

        return Result<byte[]>.Success(Encoding.UTF8.GetBytes(sb.ToString()));
    }

    private async Task<Result<BuiltCohort>> BuildStudentRowsAsync(
        Guid bundleId, Guid? subjectId, DateTime? fromUtc, DateTime? toUtc, CancellationToken ct)
    {
        var paths = await _topics.GetTopicPathsForBundlesAsync([bundleId], ct);
        if (paths.Count == 0)
            return Result<BuiltCohort>.Failure("Bundle not found or has no topics.");

        var bundleTitle = paths[0].BundleTitle;
        var topicIds = paths
            .Where(p => subjectId is null || p.SubjectId == subjectId)
            .Select(p => p.TopicId)
            .Distinct()
            .ToList();

        if (topicIds.Count == 0)
            return Result<BuiltCohort>.Failure("No topics for the selected subject.");

        var enrolledIds = await _enrollments.GetActiveUserIdsForBundleAsync(bundleId, ct);
        if (enrolledIds.Count == 0)
            return Result<BuiltCohort>.Success(new BuiltCohort(bundleTitle, []));

        var names = await _users.GetDisplayNamesAsync(enrolledIds, ct);
        var topicIdSet = topicIds.ToHashSet();

        var quizResults = await _db.QuizResults.AsNoTracking()
            .Where(r => enrolledIds.Contains(r.UserId) && topicIdSet.Contains(r.TopicId))
            .ToListAsync(ct);

        if (fromUtc is not null)
            quizResults = quizResults.Where(r => r.SubmittedAt >= fromUtc.Value).ToList();
        if (toUtc is not null)
            quizResults = quizResults.Where(r => r.SubmittedAt <= toUtc.Value).ToList();

        var videoProgress = await _db.LectureWatchProgress.AsNoTracking()
            .Where(p => enrolledIds.Contains(p.UserId) && topicIdSet.Contains(p.TopicId))
            .ToListAsync(ct);

        var lectureMap = await _lectures.GetLectureIdsByTopicAsync(topicIds, ct);
        var videosTotal = lectureMap.Values.Sum(v => v.Count);

        var quizTopicsByUser = quizResults
            .GroupBy(r => r.UserId)
            .ToDictionary(g => g.Key, g => g.Select(r => r.TopicId).ToHashSet());

        var watchedByUser = videoProgress
            .Where(p => p.ProgressPercent >= BundleCompletionService.WatchedThresholdPercent)
            .GroupBy(p => p.UserId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(p => p.LectureId).ToHashSet());

        static bool IsTopicComplete(
            Guid studentId,
            Guid topicId,
            IReadOnlyDictionary<Guid, HashSet<Guid>> quizTopicsByUser,
            IReadOnlyDictionary<Guid, HashSet<Guid>> watchedByUser,
            IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> lectureMap)
        {
            if (quizTopicsByUser.TryGetValue(studentId, out var quizTopics) && quizTopics.Contains(topicId))
                return true;
            if (!lectureMap.TryGetValue(topicId, out var lectureIds) || lectureIds.Count == 0)
                return false;
            if (!watchedByUser.TryGetValue(studentId, out var watched))
                return false;
            return lectureIds.All(watched.Contains);
        }

        var students = new List<CohortStudentRowDto>();
        foreach (var studentId in enrolledIds)
        {
            var completed = topicIds.Count(t => IsTopicComplete(studentId, t, quizTopicsByUser, watchedByUser, lectureMap));

            var studentQuizzes = quizResults.Where(r => r.UserId == studentId).ToList();
            var bestPerQuiz = studentQuizzes
                .GroupBy(r => r.QuizId)
                .Select(g => g.Max(x => x.Percentage))
                .ToList();
            var avgAccuracy = bestPerQuiz.Count == 0 ? 0 : (int)Math.Round(bestPerQuiz.Average());

            var studentVideos = videoProgress.Where(p => p.UserId == studentId).ToList();
            var watchedLectureIds = studentVideos
                .Where(p => p.ProgressPercent >= BundleCompletionService.WatchedThresholdPercent)
                .Select(p => p.LectureId)
                .ToHashSet();
            var videosWatched = lectureMap.Values.SelectMany(v => v).Count(id => watchedLectureIds.Contains(id));

            var lastActive = new[]
            {
                studentQuizzes.Count > 0 ? studentQuizzes.Max(r => r.SubmittedAt) : (DateTime?)null,
                studentVideos.Count > 0 ? studentVideos.Max(p => p.LastWatchedAt) : (DateTime?)null
            }.Where(d => d is not null).Select(d => d!.Value).DefaultIfEmpty().Max();

            var pct = topicIds.Count == 0 ? 0 : (int)Math.Round(100.0 * completed / topicIds.Count);

            students.Add(new CohortStudentRowDto(
                studentId,
                names.TryGetValue(studentId, out var n) ? n : "Unknown",
                completed,
                topicIds.Count,
                pct,
                avgAccuracy,
                videosWatched,
                videosTotal,
                lastActive == default ? null : lastActive));
        }

        return Result<BuiltCohort>.Success(new BuiltCohort(
            bundleTitle,
            students.OrderByDescending(s => s.CompletionPercent).ThenBy(s => s.StudentName).ToList()));
    }

    private static string Csv(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private sealed record BuiltCohort(string BundleTitle, IReadOnlyList<CohortStudentRowDto> Students);
}
