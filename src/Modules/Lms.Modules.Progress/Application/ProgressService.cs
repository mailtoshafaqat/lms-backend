using Lms.Modules.Progress.Infrastructure;
using Lms.Shared.Common;
using Lms.Shared.Courses;
using Lms.Shared.Enrollments;
using Lms.Shared.QnA;
using Lms.Shared.Users;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Progress.Application;

public sealed class ProgressService : IProgressService
{
    private readonly ProgressDbContext _db;
    private readonly IUserDirectory _users;
    private readonly ICourseScopeReader _scope;
    private readonly ISubjectAccessService _subjects;
    private readonly IEnrollmentReader _enrollments;
    private readonly IDoubtSummaryReader _doubts;

    public ProgressService(
        ProgressDbContext db,
        IUserDirectory users,
        ICourseScopeReader scope,
        ISubjectAccessService subjects,
        IEnrollmentReader enrollments,
        IDoubtSummaryReader doubts)
    {
        _db = db;
        _users = users;
        _scope = scope;
        _subjects = subjects;
        _enrollments = enrollments;
        _doubts = doubts;
    }

    public async Task<IReadOnlyList<GradeDto>> GetMyGradesAsync(Guid userId, CancellationToken ct = default)
    {
        var results = await _db.QuizResults
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.SubmittedAt)
            .Take(50)
            .ToListAsync(ct);

        return results
            .Select(r => new GradeDto(r.QuizId, r.TopicId, r.QuizTitle, r.Score, r.Total, r.Percentage, r.SubmittedAt))
            .ToList();
    }

    public async Task<IReadOnlyList<LeaderboardRowDto>> GetLeaderboardAsync(
        int take, Guid? currentUserId, CancellationToken ct = default)
    {
        // Points = sum of each user's best score per quiz (so re-attempting can't farm points).
        var perQuizBest = await _db.QuizResults
            .GroupBy(r => new { r.UserId, r.QuizId })
            .Select(g => new { g.Key.UserId, Best = g.Max(x => x.Score) })
            .ToListAsync(ct);

        var totals = perQuizBest
            .GroupBy(x => x.UserId)
            .Select(g => new { UserId = g.Key, Points = g.Sum(x => x.Best) })
            .OrderByDescending(x => x.Points)
            .Take(take)
            .ToList();

        var names = await _users.GetDisplayNamesAsync(totals.Select(t => t.UserId), ct);

        return totals
            .Select((t, i) => new LeaderboardRowDto(
                i + 1,
                t.UserId,
                names.TryGetValue(t.UserId, out var n) ? n : "Unknown",
                t.Points,
                currentUserId.HasValue && t.UserId == currentUserId.Value))
            .ToList();
    }

    public async Task<Result<IReadOnlyList<LeaderboardRowDto>>> GetSubjectLeaderboardAsync(
        Guid userId, string role, Guid subjectId, int take, CancellationToken ct = default)
    {
        if (!await _subjects.CanManageSubjectAsync(userId, role, subjectId, ct))
            return Result<IReadOnlyList<LeaderboardRowDto>>.Failure("Forbidden");

        var scope = await _scope.GetSubjectScopeAsync(subjectId, ct);
        if (scope is null)
            return Result<IReadOnlyList<LeaderboardRowDto>>.Failure("Subject not found.");

        var topicIds = await _scope.GetTopicIdsForSubjectAsync(subjectId, ct);
        if (topicIds.Count == 0)
            return Result<IReadOnlyList<LeaderboardRowDto>>.Success([]);

        var enrolledIds = await _enrollments.GetActiveUserIdsForBundleAsync(scope.BundleId, ct);
        if (enrolledIds.Count == 0)
            return Result<IReadOnlyList<LeaderboardRowDto>>.Success([]);

        var size = take <= 5 ? 5 : 10;
        var topicIdSet = topicIds.ToHashSet();

        var perQuizBest = await _db.QuizResults.AsNoTracking()
            .Where(r => topicIdSet.Contains(r.TopicId) && enrolledIds.Contains(r.UserId))
            .GroupBy(r => new { r.UserId, r.QuizId })
            .Select(g => new { g.Key.UserId, Best = g.Max(x => x.Score) })
            .ToListAsync(ct);

        var totals = perQuizBest
            .GroupBy(x => x.UserId)
            .Select(g => new { UserId = g.Key, Points = g.Sum(x => x.Best) })
            .OrderByDescending(x => x.Points)
            .Take(size)
            .ToList();

        var names = await _users.GetDisplayNamesAsync(totals.Select(t => t.UserId), ct);

        var rows = totals
            .Select((t, i) => new LeaderboardRowDto(
                i + 1,
                t.UserId,
                names.TryGetValue(t.UserId, out var n) ? n : "Unknown",
                t.Points,
                false))
            .ToList();

        return Result<IReadOnlyList<LeaderboardRowDto>>.Success(rows);
    }

    public async Task<Result<SubjectProgressDto>> GetSubjectProgressAsync(
        Guid userId, string role, Guid subjectId, CancellationToken ct = default)
    {
        if (!await _subjects.CanManageSubjectAsync(userId, role, subjectId, ct))
            return Result<SubjectProgressDto>.Failure("Forbidden");

        var scope = await _scope.GetSubjectScopeAsync(subjectId, ct);
        if (scope is null)
            return Result<SubjectProgressDto>.Failure("Subject not found.");

        var topicIds = await _scope.GetTopicIdsForSubjectAsync(subjectId, ct);
        var enrolledIds = await _enrollments.GetActiveUserIdsForBundleAsync(scope.BundleId, ct);

        if (enrolledIds.Count == 0)
        {
            return Result<SubjectProgressDto>.Success(new SubjectProgressDto(
                subjectId, scope.SubjectTitle, []));
        }

        var results = topicIds.Count == 0
            ? []
            : await _db.QuizResults.AsNoTracking()
                .Where(r => topicIds.Contains(r.TopicId) && enrolledIds.Contains(r.UserId))
                .ToListAsync(ct);

        var names = await _users.GetDisplayNamesAsync(enrolledIds, ct);

        var students = enrolledIds
            .Select(studentId => BuildStudentProgress(studentId, names, results))
            .OrderByDescending(s => s.QuizzesCompleted)
            .ThenBy(s => s.StudentName)
            .ToList();

        return Result<SubjectProgressDto>.Success(new SubjectProgressDto(
            subjectId, scope.SubjectTitle, students));
    }

    public async Task<Result<StudentDetailDto>> GetStudentDetailAsync(
        Guid userId, string role, Guid subjectId, Guid studentUserId, CancellationToken ct = default)
    {
        if (!await _subjects.CanManageSubjectAsync(userId, role, subjectId, ct))
            return Result<StudentDetailDto>.Failure("Forbidden");

        var scope = await _scope.GetSubjectScopeAsync(subjectId, ct);
        if (scope is null)
            return Result<StudentDetailDto>.Failure("Subject not found.");

        var enrolledIds = await _enrollments.GetActiveUserIdsForBundleAsync(scope.BundleId, ct);
        if (!enrolledIds.Contains(studentUserId))
            return Result<StudentDetailDto>.Failure("Student not enrolled in this subject's course.");

        var topicIds = await _scope.GetTopicIdsForSubjectAsync(subjectId, ct);
        var topicIdSet = topicIds.ToHashSet();

        var results = topicIdSet.Count == 0
            ? []
            : await _db.QuizResults.AsNoTracking()
                .Where(r => r.UserId == studentUserId && topicIdSet.Contains(r.TopicId))
                .ToListAsync(ct);

        var names = await _users.GetDisplayNamesAsync([studentUserId], ct);
        var progress = BuildStudentProgress(studentUserId, names, results);

        var mistakeRows = await _db.MistakeEntries.AsNoTracking()
            .Where(m => m.UserId == studentUserId && topicIdSet.Contains(m.TopicId))
            .ToListAsync(ct);

        var unresolved = mistakeRows.Count(m => m.ResolvedAt is null);
        var totalWrong = mistakeRows.Sum(m => m.TimesWrong);
        var lastMistake = mistakeRows.Count == 0 ? (DateTime?)null : mistakeRows.Max(m => m.LastSeenAt);

        var doubtSummary = await _doubts.GetSummaryForStudentSubjectAsync(studentUserId, subjectId, ct);
        var doubts = new StudentDoubtSummaryDto(
            doubtSummary.OpenCount, doubtSummary.ResolvedCount, doubtSummary.LastActivityAt);

        var mistakes = new StudentMistakeSummaryDto(unresolved, totalWrong, lastMistake);

        var lastActive = new[]
        {
            progress.Results.Count > 0 ? progress.Results.Max(r => r.SubmittedAt) : (DateTime?)null,
            doubtSummary.LastActivityAt,
            lastMistake
        }.Where(d => d is not null).Select(d => d!.Value).DefaultIfEmpty().Max();

        return Result<StudentDetailDto>.Success(new StudentDetailDto(
            studentUserId,
            progress.StudentName,
            subjectId,
            scope.SubjectTitle,
            progress.QuizzesCompleted,
            progress.AveragePercentage,
            progress.Results,
            doubts,
            mistakes,
            lastActive == default ? null : lastActive));
    }

    public async Task<DashboardOverviewDto> GetDashboardOverviewAsync(
        Guid userId, CancellationToken ct = default)
    {
        var results = await _db.QuizResults.AsNoTracking()
            .Where(r => r.UserId == userId)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var weekStart = now.Date.AddDays(-6);
        var prevWeekStart = now.Date.AddDays(-13);

        var bestPerQuiz = results
            .GroupBy(r => r.QuizId)
            .Select(g => g.OrderByDescending(x => x.Percentage).ThenByDescending(x => x.SubmittedAt).First())
            .ToList();

        var overallAccuracy = bestPerQuiz.Count == 0
            ? 0
            : (int)Math.Round(bestPerQuiz.Average(r => r.Percentage));

        var thisWeekAttempts = results.Where(r => r.SubmittedAt >= weekStart).ToList();
        var prevWeekAttempts = results
            .Where(r => r.SubmittedAt >= prevWeekStart && r.SubmittedAt < weekStart)
            .ToList();

        static int AvgPct(IReadOnlyList<Domain.QuizResult> rows) =>
            rows.Count == 0 ? 0 : (int)Math.Round(rows.Average(r => r.Percentage));

        var accuracyChange = AvgPct(thisWeekAttempts) - AvgPct(prevWeekAttempts);

        var mcqsThisMonth = results
            .Where(r => r.SubmittedAt >= monthStart)
            .Sum(r => r.Total);

        var activityDays = results
            .Select(r => r.SubmittedAt.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();

        var practiceStreak = ComputeStreak(activityDays, now.Date);

        var topicIds = results.Select(r => r.TopicId).Distinct().ToList();
        var topicScopes = await _scope.GetTopicScopesAsync(topicIds, ct);

        var subjectAccuracy = bestPerQuiz
            .Where(r => topicScopes.ContainsKey(r.TopicId))
            .GroupBy(r => topicScopes[r.TopicId].SubjectId)
            .Select(g =>
            {
                var scope = topicScopes[g.First().TopicId];
                var quizzes = g.ToList();
                return new SubjectAccuracyDto(
                    g.Key,
                    scope.SubjectTitle,
                    (int)Math.Round(quizzes.Average(x => x.Percentage)),
                    quizzes.Count);
            })
            .OrderByDescending(s => s.Accuracy)
            .ToList();

        var weakestSubject = subjectAccuracy.Count == 0
            ? null
            : subjectAccuracy.MinBy(s => s.Accuracy);

        var weeklyTrend = Enumerable.Range(0, 7)
            .Select(offset =>
            {
                var day = now.Date.AddDays(-6 + offset);
                var dayResults = results.Where(r => r.SubmittedAt.Date == day).ToList();
                return new WeeklyScoreDto(
                    day.ToString("ddd"),
                    AvgPct(dayResults),
                    dayResults.Count);
            })
            .ToList();

        var perQuizBestAll = await _db.QuizResults.AsNoTracking()
            .GroupBy(r => new { r.UserId, r.QuizId })
            .Select(g => new { g.Key.UserId, Best = g.Max(x => x.Score) })
            .ToListAsync(ct);

        var instituteTotals = perQuizBestAll
            .GroupBy(x => x.UserId)
            .Select(g => new { UserId = g.Key, Points = g.Sum(x => x.Best) })
            .OrderByDescending(x => x.Points)
            .ToList();

        var instituteStudentCount = instituteTotals.Count;
        int? instituteRank = null;
        if (instituteTotals.Count > 0)
        {
            var idx = instituteTotals.FindIndex(x => x.UserId == userId);
            if (idx >= 0) instituteRank = idx + 1;
        }

        var enrollments = await _enrollments.GetActiveEnrollmentsAsync(userId, ct);
        var bundleIds = enrollments.Select(e => e.BundleId).ToList();
        var topicCounts = await _scope.GetTopicCountsByBundleAsync(bundleIds, ct);

        var topicsAttemptedByBundle = new Dictionary<Guid, HashSet<Guid>>();
        foreach (var bundleId in bundleIds)
            topicsAttemptedByBundle[bundleId] = [];

        foreach (var result in results)
        {
            if (!topicScopes.TryGetValue(result.TopicId, out var scope)) continue;
            if (topicsAttemptedByBundle.TryGetValue(scope.BundleId, out var set))
                set.Add(result.TopicId);
        }

        var bundleProgress = enrollments.Select(e =>
        {
            var total = topicCounts.TryGetValue(e.BundleId, out var count) ? count : 0;
            var completed = topicsAttemptedByBundle.TryGetValue(e.BundleId, out var set) ? set.Count : 0;
            var pct = total == 0 ? 0 : (int)Math.Round(100.0 * completed / total);
            return new BundleProgressDto(e.BundleId, e.BundleTitle, completed, total, pct);
        }).ToList();

        return new DashboardOverviewDto(
            overallAccuracy,
            accuracyChange,
            mcqsThisMonth,
            instituteRank,
            instituteStudentCount,
            practiceStreak,
            subjectAccuracy,
            weeklyTrend,
            bundleProgress,
            weakestSubject);
    }

    private static int ComputeStreak(IReadOnlyList<DateTime> activityDaysDescending, DateTime todayUtc)
    {
        if (activityDaysDescending.Count == 0) return 0;

        var daySet = activityDaysDescending.ToHashSet();
        var cursor = daySet.Contains(todayUtc) ? todayUtc : todayUtc.AddDays(-1);
        if (!daySet.Contains(cursor)) return 0;

        var streak = 0;
        while (daySet.Contains(cursor))
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }

        return streak;
    }

    private static StudentSubjectProgressDto BuildStudentProgress(
        Guid studentId,
        IReadOnlyDictionary<Guid, string> names,
        IReadOnlyList<Domain.QuizResult> results)
    {
        var userResults = results.Where(r => r.UserId == studentId).ToList();
        var bestPerQuiz = userResults
            .GroupBy(r => r.QuizId)
            .Select(g =>
            {
                var best = g
                    .OrderByDescending(x => x.Percentage)
                    .ThenByDescending(x => x.SubmittedAt)
                    .First();
                return new SubjectQuizResultDto(
                    best.QuizId,
                    best.TopicId,
                    best.QuizTitle,
                    best.Score,
                    best.Total,
                    best.Percentage,
                    best.SubmittedAt);
            })
            .OrderByDescending(x => x.SubmittedAt)
            .ToList();

        var average = bestPerQuiz.Count == 0
            ? 0
            : (int)Math.Round(bestPerQuiz.Average(x => x.Percentage));

        return new StudentSubjectProgressDto(
            studentId,
            names.TryGetValue(studentId, out var name) ? name : "Unknown",
            bestPerQuiz.Count,
            average,
            bestPerQuiz);
    }
}
