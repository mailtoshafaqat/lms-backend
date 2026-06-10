using Lms.Modules.Courses.Domain;
using Lms.Modules.Courses.Infrastructure;
using Lms.Shared.Auth;
using Lms.Shared.Common;
using Lms.Shared.Courses;
using Lms.Shared.Tenancy;
using Lms.Shared.Users;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Courses.Application;

public sealed class SubjectAccessService : ISubjectAccessService
{
    private readonly CoursesDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IInstituteUserReader _users;

    public SubjectAccessService(CoursesDbContext db, ITenantContext tenant, IInstituteUserReader users)
    {
        _db = db;
        _tenant = tenant;
        _users = users;
    }

    public bool HasInstituteWideAccess(string? role) =>
        role is Roles.SuperAdmin or Roles.InstituteAdmin;

    public async Task<bool> CanManageSubjectAsync(
        Guid userId, string role, Guid subjectId, CancellationToken ct = default)
    {
        if (HasInstituteWideAccess(role)) return true;
        if (role != Roles.Teacher) return false;

        return await _db.SubjectTeachers.AnyAsync(
            a => a.UserId == userId && a.SubjectId == subjectId, ct);
    }

    public Task<bool> IsTeacherAssignedAsync(Guid teacherUserId, Guid subjectId, CancellationToken ct = default) =>
        _db.SubjectTeachers.AnyAsync(
            a => a.UserId == teacherUserId && a.SubjectId == subjectId, ct);

    public async Task<IReadOnlyList<Guid>> GetTeacherIdsForSubjectAsync(
        Guid subjectId, CancellationToken ct = default) =>
        await _db.SubjectTeachers.AsNoTracking()
            .Where(a => a.SubjectId == subjectId)
            .Select(a => a.UserId)
            .ToListAsync(ct);

    public async Task<bool> CanManageUnitAsync(
        Guid userId, string role, Guid unitId, CancellationToken ct = default)
    {
        if (HasInstituteWideAccess(role)) return true;
        if (role != Roles.Teacher) return false;

        var subjectId = await _db.Units
            .Where(u => u.Id == unitId)
            .Select(u => u.SubjectId)
            .FirstOrDefaultAsync(ct);

        return subjectId != Guid.Empty
            && await IsTeacherAssignedAsync(userId, subjectId, ct);
    }

    public async Task<bool> CanManageTopicAsync(
        Guid userId, string role, Guid topicId, CancellationToken ct = default)
    {
        if (HasInstituteWideAccess(role)) return true;
        if (role != Roles.Teacher) return false;

        var subjectId = await _db.Topics
            .Where(t => t.Id == topicId)
            .Select(t => t.Unit!.SubjectId)
            .FirstOrDefaultAsync(ct);

        return subjectId != Guid.Empty
            && await _db.SubjectTeachers.AnyAsync(
                a => a.UserId == userId && a.SubjectId == subjectId, ct);
    }

    public async Task<IReadOnlyList<AssignedSubjectDto>> GetAssignedSubjectsAsync(
        Guid userId, string role, CancellationToken ct = default)
    {
        var query = _db.Subjects.AsQueryable();

        if (!HasInstituteWideAccess(role))
        {
            if (role != Roles.Teacher) return [];

            var ids = await _db.SubjectTeachers
                .Where(a => a.UserId == userId)
                .Select(a => a.SubjectId)
                .ToListAsync(ct);

            if (ids.Count == 0) return [];
            query = query.Where(s => ids.Contains(s.Id));
        }

        return await query
            .OrderBy(s => s.Order)
            .Select(s => new AssignedSubjectDto(
                s.Id,
                s.Title,
                s.BundleId,
                s.Bundle!.Title))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<TeacherSubjectAssignmentDto>> ListAssignmentsAsync(
        CancellationToken ct = default)
    {
        var rows = await _db.SubjectTeachers
            .GroupBy(a => a.UserId)
            .Select(g => new { UserId = g.Key, SubjectIds = g.Select(x => x.SubjectId).ToList() })
            .ToListAsync(ct);

        return rows
            .Select(r => new TeacherSubjectAssignmentDto(r.UserId, r.SubjectIds))
            .ToList();
    }

    public async Task<Result> SetTeacherSubjectsAsync(
        Guid teacherUserId, IReadOnlyList<Guid> subjectIds, CancellationToken ct = default)
    {
        if (teacherUserId == Guid.Empty)
            return Result.Failure("Teacher is required.");

        if (!await _users.IsActiveTeacherAsync(teacherUserId, ct))
            return Result.Failure("User is not an active teacher on this institute.");

        var distinct = subjectIds.Distinct().ToList();
        if (distinct.Count > 0)
        {
            var found = await _db.Subjects.CountAsync(s => distinct.Contains(s.Id), ct);
            if (found != distinct.Count)
                return Result.Failure("One or more subjects were not found.");
        }

        var existing = await _db.SubjectTeachers
            .Where(a => a.UserId == teacherUserId)
            .ToListAsync(ct);

        _db.SubjectTeachers.RemoveRange(existing);

        foreach (var subjectId in distinct)
        {
            _db.SubjectTeachers.Add(new SubjectTeacher
            {
                TenantId = _tenant.TenantId,
                UserId = teacherUserId,
                SubjectId = subjectId
            });
        }

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
