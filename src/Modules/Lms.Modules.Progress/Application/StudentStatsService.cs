using Lms.Shared.Courses;
using Lms.Shared.Enrollments;

namespace Lms.Modules.Progress.Application;

public interface IStudentStatsService
{
    Task<StudentStatsDto> GetMyStatsAsync(Guid userId, CancellationToken ct = default);
}

public sealed record SubjectCompletionDto(
    Guid SubjectId,
    string SubjectTitle,
    int TopicsCompleted,
    int TopicsTotal,
    int PercentComplete);

public sealed record StudentStatsDto(
    int OverallAccuracy,
    int AccuracyChangeThisWeek,
    int McqsAttemptedThisMonth,
    int PracticeStreakDays,
    IReadOnlyList<SubjectCompletionDto> SubjectCompletion,
    IReadOnlyList<WeeklyScoreDto> WeeklyTrend);

public sealed class StudentStatsService : IStudentStatsService
{
    private readonly IProgressService _progress;
    private readonly IEnrollmentReader _enrollments;
    private readonly IEnrolledSubjectsReader _subjects;
    private readonly ICourseScopeReader _scope;
    private readonly BundleCompletionService _completion;

    public StudentStatsService(
        IProgressService progress,
        IEnrollmentReader enrollments,
        IEnrolledSubjectsReader subjects,
        ICourseScopeReader scope,
        BundleCompletionService completion)
    {
        _progress = progress;
        _enrollments = enrollments;
        _subjects = subjects;
        _scope = scope;
        _completion = completion;
    }

    public async Task<StudentStatsDto> GetMyStatsAsync(Guid userId, CancellationToken ct = default)
    {
        var dashboard = await _progress.GetDashboardOverviewAsync(userId, ct);
        var enrolledSubjects = await _subjects.GetEnrolledSubjectsAsync(userId, ct);

        var subjectCompletion = new List<SubjectCompletionDto>();
        foreach (var subject in enrolledSubjects)
        {
            var topics = await _scope.GetOrderedTopicsForSubjectAsync(subject.SubjectId, ct);
            var completed = 0;
            foreach (var topic in topics)
            {
                if (await _completion.IsTopicCompleteAsync(userId, topic.TopicId, ct))
                    completed++;
            }

            var total = topics.Count;
            var pct = total == 0 ? 0 : (int)Math.Round(100.0 * completed / total);
            subjectCompletion.Add(new SubjectCompletionDto(
                subject.SubjectId,
                subject.SubjectTitle,
                completed,
                total,
                pct));
        }

        return new StudentStatsDto(
            dashboard.OverallAccuracy,
            dashboard.AccuracyChangeThisWeek,
            dashboard.McqsAttemptedThisMonth,
            dashboard.PracticeStreakDays,
            subjectCompletion,
            dashboard.WeeklyTrend);
    }
}
