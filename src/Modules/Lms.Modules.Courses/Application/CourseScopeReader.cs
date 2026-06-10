using Lms.Modules.Courses.Infrastructure;
using Lms.Shared.Courses;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Courses.Application;

public sealed class CourseScopeReader : ICourseScopeReader
{
    private readonly CoursesDbContext _db;

    public CourseScopeReader(CoursesDbContext db) => _db = db;

    public async Task<TopicScope?> GetTopicScopeAsync(Guid topicId, CancellationToken ct = default) =>
        await _db.Topics.AsNoTracking()
            .Where(t => t.Id == topicId)
            .Select(t => new TopicScope(
                t.Id,
                t.Title,
                t.Unit!.SubjectId,
                t.Unit.Subject!.Title,
                t.Unit.Subject.BundleId))
            .FirstOrDefaultAsync(ct);

    public async Task<SubjectScope?> GetSubjectScopeAsync(Guid subjectId, CancellationToken ct = default) =>
        await _db.Subjects.AsNoTracking()
            .Where(s => s.Id == subjectId)
            .Select(s => new SubjectScope(s.Id, s.Title, s.BundleId))
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Guid>> GetTopicIdsForSubjectAsync(Guid subjectId, CancellationToken ct = default) =>
        await _db.Topics.AsNoTracking()
            .Where(t => t.Unit!.SubjectId == subjectId)
            .Select(t => t.Id)
            .ToListAsync(ct);
}
