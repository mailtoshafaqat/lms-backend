using Lms.Modules.Progress.Infrastructure;
using Lms.Shared.Courses;
using Lms.Shared.Enrollments;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Progress.Application;

public interface IStudentProgramService
{
    Task<StudentProgramDto> GetMyProgramAsync(Guid userId, CancellationToken ct = default);
}

public sealed record ProgramSubjectDto(
    Guid SubjectId,
    string Title,
    int TopicsCompleted,
    int TopicsTotal,
    int PercentComplete);

public sealed record ProgramBundleDto(
    Guid BundleId,
    string Title,
    IReadOnlyList<ProgramSubjectDto> Subjects);

public sealed record ContinueTopicDto(
    Guid TopicId,
    string TopicTitle,
    string SubjectTitle,
    string BundleTitle);

public sealed record StudentProgramDto(
    IReadOnlyList<ProgramBundleDto> Bundles,
    ContinueTopicDto? ContinueTopic);

public sealed class StudentProgramService : IStudentProgramService
{
    private readonly IEnrolledSubjectsReader _subjects;
    private readonly ICourseScopeReader _scope;
    private readonly BundleCompletionService _completion;
    private readonly ProgressDbContext _db;

    public StudentProgramService(
        IEnrolledSubjectsReader subjects,
        ICourseScopeReader scope,
        BundleCompletionService completion,
        ProgressDbContext db)
    {
        _subjects = subjects;
        _scope = scope;
        _completion = completion;
        _db = db;
    }

    public async Task<StudentProgramDto> GetMyProgramAsync(Guid userId, CancellationToken ct = default)
    {
        var enrolledSubjects = await _subjects.GetEnrolledSubjectsAsync(userId, ct);
        var syllabusTopics = new List<(ContinueTopicDto Ref, int SubjectIndex, int TopicIndex)>();
        var bundles = new List<ProgramBundleDto>();

        foreach (var bundleGroup in enrolledSubjects.GroupBy(s => s.BundleId))
        {
            var bundleTitle = bundleGroup.First().BundleTitle;
            var subjects = new List<ProgramSubjectDto>();
            var subjectIndex = 0;

            foreach (var subject in bundleGroup)
            {
                var topics = await _scope.GetOrderedTopicsForSubjectAsync(subject.SubjectId, ct);
                var completed = 0;
                for (var i = 0; i < topics.Count; i++)
                {
                    var topic = topics[i];
                    if (await _completion.IsTopicCompleteAsync(userId, topic.TopicId, ct))
                        completed++;

                    syllabusTopics.Add((
                        new ContinueTopicDto(
                            topic.TopicId,
                            topic.TopicTitle,
                            subject.SubjectTitle,
                            bundleTitle),
                        subjectIndex,
                        i));
                }

                var total = topics.Count;
                var pct = total == 0 ? 0 : (int)Math.Round(100.0 * completed / total);
                subjects.Add(new ProgramSubjectDto(
                    subject.SubjectId, subject.SubjectTitle, completed, total, pct));
                subjectIndex++;
            }

            bundles.Add(new ProgramBundleDto(bundleGroup.Key, bundleTitle, subjects));
        }

        var continueTopic = await ResolveContinueTopicAsync(userId, syllabusTopics, ct);
        return new StudentProgramDto(bundles, continueTopic);
    }

    private async Task<ContinueTopicDto?> ResolveContinueTopicAsync(
        Guid userId,
        IReadOnlyList<(ContinueTopicDto Ref, int SubjectIndex, int TopicIndex)> syllabusTopics,
        CancellationToken ct)
    {
        var recentWatch = await _db.LectureWatchProgress.AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.LastWatchedAt)
            .Select(p => p.TopicId)
            .FirstOrDefaultAsync(ct);

        if (recentWatch != Guid.Empty
            && !await _completion.IsTopicCompleteAsync(userId, recentWatch, ct))
        {
            var match = syllabusTopics.FirstOrDefault(t => t.Ref.TopicId == recentWatch);
            if (match.Ref is not null) return match.Ref;
        }

        foreach (var entry in syllabusTopics.OrderBy(t => t.SubjectIndex).ThenBy(t => t.TopicIndex))
        {
            if (!await _completion.IsTopicCompleteAsync(userId, entry.Ref.TopicId, ct))
                return entry.Ref;
        }

        return null;
    }
}
