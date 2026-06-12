using Lms.Modules.Courses.Domain;
using Lms.Modules.Courses.Infrastructure;
using Lms.Shared.Auth;
using Lms.Shared.Common;
using Lms.Shared.Courses;
using Lms.Shared.Tenancy;
using Lms.Shared.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Courses.Application;

public sealed class SubjectAccessService : ISubjectAccessService
{
    private readonly CoursesDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IInstituteUserReader _users;
    private readonly IHttpContextAccessor _http;

    public SubjectAccessService(
        CoursesDbContext db,
        ITenantContext tenant,
        IInstituteUserReader users,
        IHttpContextAccessor http)
    {
        _db = db;
        _tenant = tenant;
        _users = users;
        _http = http;
    }

    public bool HasInstituteWideAccess(string? role)
    {
        if (role is Roles.SuperAdmin or Roles.InstituteAdmin) return true;
        return _http.HttpContext?.User.HasInstituteWideAccess() ?? false;
    }

    public async Task<bool> CanManageSubjectAsync(
        Guid userId, string role, Guid subjectId, CancellationToken ct = default)
    {
        if (HasInstituteWideAccess(role)) return true;
        if (role != Roles.Teacher) return false;

        if (await _db.SubjectTeachers.AnyAsync(
                a => a.UserId == userId && a.SubjectId == subjectId, ct))
            return true;

        var definitionId = await _db.Subjects
            .Where(s => s.Id == subjectId)
            .Select(s => s.SubjectDefinitionId)
            .FirstOrDefaultAsync(ct);

        return definitionId is not null
            && await IsTeacherAssignedToDefinitionAsync(userId, definitionId.Value, ct);
    }

    public Task<bool> IsTeacherAssignedAsync(Guid teacherUserId, Guid subjectId, CancellationToken ct = default) =>
        CanManageSubjectAsync(teacherUserId, Roles.Teacher, subjectId, ct);

    public async Task<IReadOnlyList<Guid>> GetTeacherIdsForSubjectAsync(
        Guid subjectId, CancellationToken ct = default)
    {
        var direct = await _db.SubjectTeachers.AsNoTracking()
            .Where(a => a.SubjectId == subjectId)
            .Select(a => a.UserId)
            .ToListAsync(ct);

        var definitionId = await _db.Subjects
            .Where(s => s.Id == subjectId)
            .Select(s => s.SubjectDefinitionId)
            .FirstOrDefaultAsync(ct);

        if (definitionId is null) return direct;

        var catalog = await _db.SubjectDefinitionTeachers.AsNoTracking()
            .Where(a => a.SubjectDefinitionId == definitionId)
            .Select(a => a.UserId)
            .ToListAsync(ct);

        return direct.Concat(catalog).Distinct().ToList();
    }

    public async Task<bool> CanManageUnitAsync(
        Guid userId, string role, Guid unitId, CancellationToken ct = default)
    {
        if (HasInstituteWideAccess(role)) return true;
        if (role != Roles.Teacher) return false;

        var unit = await _db.Units.AsNoTracking()
            .Where(u => u.Id == unitId)
            .Select(u => new { u.SubjectId, u.SubjectDefinitionId })
            .FirstOrDefaultAsync(ct);

        if (unit is null) return false;

        if (unit.SubjectDefinitionId is Guid defId
            && await IsTeacherAssignedToDefinitionAsync(userId, defId, ct))
            return true;

        if (unit.SubjectId is Guid subjectId
            && await CanManageSubjectAsync(userId, role, subjectId, ct))
            return true;

        var linkedSubjectIds = await _db.SubjectSharedUnits
            .Where(l => l.UnitId == unitId)
            .Select(l => l.SubjectId)
            .ToListAsync(ct);

        foreach (var linkedSubjectId in linkedSubjectIds)
        {
            if (await CanManageSubjectAsync(userId, role, linkedSubjectId, ct))
                return true;
        }

        return false;
    }

    public async Task<bool> CanManageTopicAsync(
        Guid userId, string role, Guid topicId, CancellationToken ct = default)
    {
        if (HasInstituteWideAccess(role)) return true;
        if (role != Roles.Teacher) return false;

        var unitId = await _db.Topics
            .Where(t => t.Id == topicId)
            .Select(t => t.UnitId)
            .FirstOrDefaultAsync(ct);

        return unitId != Guid.Empty
            && await CanManageUnitAsync(userId, role, unitId, ct);
    }

    public async Task<IReadOnlyList<AssignedSubjectDto>> GetAssignedSubjectsAsync(
        Guid userId, string role, CancellationToken ct = default)
    {
        var query = _db.Subjects.AsQueryable();

        if (!HasInstituteWideAccess(role))
        {
            if (role != Roles.Teacher) return [];

            var directIds = await _db.SubjectTeachers
                .Where(a => a.UserId == userId)
                .Select(a => a.SubjectId)
                .ToListAsync(ct);

            var catalogIds = await _db.SubjectDefinitionTeachers
                .Where(a => a.UserId == userId)
                .Select(a => a.SubjectDefinitionId)
                .ToListAsync(ct);

            if (directIds.Count == 0 && catalogIds.Count == 0) return [];

            query = query.Where(s =>
                directIds.Contains(s.Id)
                || (s.SubjectDefinitionId != null && catalogIds.Contains(s.SubjectDefinitionId.Value)));
        }

        return await query
            .OrderBy(s => s.Order)
            .Select(s => new AssignedSubjectDto(
                s.Id,
                s.Title,
                s.BundleId,
                s.Bundle!.Title,
                s.SubjectDefinitionId,
                s.SubjectDefinition != null ? s.SubjectDefinition.DisplayName : null))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<CatalogSubjectGroupDto>> GetCatalogSubjectGroupsAsync(
        CancellationToken ct = default)
    {
        var definitions = await _db.SubjectDefinitions.AsNoTracking()
            .Where(d => d.IsActive)
            .OrderBy(d => d.SortOrder)
            .ToListAsync(ct);

        var placements = await _db.Subjects.AsNoTracking()
            .OrderBy(s => s.Order)
            .Select(s => new AssignedSubjectDto(
                s.Id,
                s.Title,
                s.BundleId,
                s.Bundle!.Title,
                s.SubjectDefinitionId,
                s.SubjectDefinition != null ? s.SubjectDefinition.DisplayName : null))
            .ToListAsync(ct);

        var unlinked = placements.Where(p => p.SubjectDefinitionId is null).ToList();
        var groups = definitions
            .Select(d => new CatalogSubjectGroupDto(
                d.Id,
                d.Code,
                d.DisplayName,
                placements.Where(p => p.SubjectDefinitionId == d.Id).ToList()))
            .ToList();

        if (unlinked.Count > 0)
        {
            groups.Add(new CatalogSubjectGroupDto(
                Guid.Empty,
                "legacy",
                "Legacy (no catalog link)",
                unlinked));
        }

        return groups;
    }

    public async Task<IReadOnlyList<TeacherSubjectAssignmentDto>> ListAssignmentsAsync(
        CancellationToken ct = default)
    {
        var subjectRows = await _db.SubjectTeachers
            .GroupBy(a => a.UserId)
            .Select(g => new { UserId = g.Key, SubjectIds = g.Select(x => x.SubjectId).ToList() })
            .ToListAsync(ct);

        var catalogRows = await _db.SubjectDefinitionTeachers
            .GroupBy(a => a.UserId)
            .Select(g => new { UserId = g.Key, DefinitionIds = g.Select(x => x.SubjectDefinitionId).ToList() })
            .ToListAsync(ct);

        var userIds = subjectRows.Select(r => r.UserId)
            .Concat(catalogRows.Select(r => r.UserId))
            .Distinct();

        return userIds
            .Select(userId => new TeacherSubjectAssignmentDto(
                userId,
                subjectRows.FirstOrDefault(r => r.UserId == userId)?.SubjectIds ?? [],
                catalogRows.FirstOrDefault(r => r.UserId == userId)?.DefinitionIds ?? []))
            .ToList();
    }

    public async Task<Result> SetTeacherSubjectsAsync(
        Guid teacherUserId,
        IReadOnlyList<Guid> subjectIds,
        IReadOnlyList<Guid> subjectDefinitionIds,
        CancellationToken ct = default)
    {
        if (teacherUserId == Guid.Empty)
            return Result.Failure("Teacher is required.");

        if (!await _users.IsActiveTeacherAsync(teacherUserId, ct))
            return Result.Failure("User is not an active teacher on this institute.");

        var distinctSubjects = subjectIds.Distinct().ToList();
        if (distinctSubjects.Count > 0)
        {
            var found = await _db.Subjects.CountAsync(s => distinctSubjects.Contains(s.Id), ct);
            if (found != distinctSubjects.Count)
                return Result.Failure("One or more subjects were not found.");
        }

        var distinctDefinitions = subjectDefinitionIds.Distinct().ToList();
        if (distinctDefinitions.Count > 0)
        {
            var foundDefs = await _db.SubjectDefinitions
                .CountAsync(d => distinctDefinitions.Contains(d.Id), ct);
            if (foundDefs != distinctDefinitions.Count)
                return Result.Failure("One or more catalog subjects were not found.");
        }

        var existingSubjects = await _db.SubjectTeachers
            .Where(a => a.UserId == teacherUserId)
            .ToListAsync(ct);
        _db.SubjectTeachers.RemoveRange(existingSubjects);

        foreach (var subjectId in distinctSubjects)
        {
            _db.SubjectTeachers.Add(new SubjectTeacher
            {
                TenantId = _tenant.TenantId,
                UserId = teacherUserId,
                SubjectId = subjectId
            });
        }

        var existingDefinitions = await _db.SubjectDefinitionTeachers
            .Where(a => a.UserId == teacherUserId)
            .ToListAsync(ct);
        _db.SubjectDefinitionTeachers.RemoveRange(existingDefinitions);

        foreach (var definitionId in distinctDefinitions)
        {
            _db.SubjectDefinitionTeachers.Add(new SubjectDefinitionTeacher
            {
                TenantId = _tenant.TenantId,
                UserId = teacherUserId,
                SubjectDefinitionId = definitionId
            });
        }

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    private Task<bool> IsTeacherAssignedToDefinitionAsync(
        Guid userId, Guid definitionId, CancellationToken ct) =>
        _db.SubjectDefinitionTeachers.AnyAsync(
            a => a.UserId == userId && a.SubjectDefinitionId == definitionId, ct);
}
