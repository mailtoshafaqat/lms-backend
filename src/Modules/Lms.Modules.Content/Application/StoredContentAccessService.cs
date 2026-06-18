using Lms.Modules.Content.Infrastructure;
using Lms.Shared.Auth;
using Lms.Shared.Courses;
using Lms.Shared.Enrollments;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Content.Application;

public interface IStoredContentAccessService
{
    Task<bool> CanDownloadAsync(Guid? userId, string? role, string storageKey, CancellationToken ct = default);
}

public sealed class StoredContentAccessService : IStoredContentAccessService
{
    private readonly ContentDbContext _db;
    private readonly ICourseScopeReader _scope;
    private readonly IEnrollmentAccessGuard _enrollment;

    public StoredContentAccessService(
        ContentDbContext db,
        ICourseScopeReader scope,
        IEnrollmentAccessGuard enrollment)
    {
        _db = db;
        _scope = scope;
        _enrollment = enrollment;
    }

    public async Task<bool> CanDownloadAsync(
        Guid? userId, string? role, string storageKey, CancellationToken ct = default)
    {
        if (!StoredFilePaths.RequiresAuthentication(storageKey))
            return true;

        if (userId is null)
            return false;

        var topicId = await ResolveTopicIdAsync(storageKey, ct);
        if (topicId is null)
            return role is Roles.SuperAdmin or Roles.InstituteAdmin or Roles.Teacher;

        var topicScope = await _scope.GetTopicScopeAsync(topicId.Value, ct);
        if (topicScope is null)
            return role is Roles.SuperAdmin or Roles.InstituteAdmin or Roles.Teacher;

        return await _enrollment.HasBundleAccessAsync(userId, role, topicScope.BundleId, ct);
    }

    private async Task<Guid?> ResolveTopicIdAsync(string storageKey, CancellationToken ct)
    {
        if (StoredFilePaths.IsLectureKey(storageKey))
        {
            return await _db.Lectures.AsNoTracking()
                .Where(l => l.StorageKey == storageKey)
                .Select(l => (Guid?)l.TopicId)
                .FirstOrDefaultAsync(ct);
        }

        if (StoredFilePaths.IsNoteKey(storageKey))
        {
            return await _db.Notes.AsNoTracking()
                .Where(n => n.StorageKey == storageKey)
                .Select(n => (Guid?)n.TopicId)
                .FirstOrDefaultAsync(ct);
        }

        return null;
    }
}
