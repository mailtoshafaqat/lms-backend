using System.Text.RegularExpressions;
using Lms.Modules.Courses.Domain;
using Lms.Modules.Courses.Infrastructure;
using Lms.Shared.Common;
using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Courses.Application;

public sealed class SubjectDefinitionService : ISubjectDefinitionService
{
    private readonly CoursesDbContext _db;
    private readonly ITenantContext _tenant;

    public SubjectDefinitionService(CoursesDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<SubjectDefinitionDto>> ListAsync(
        bool activeOnly = false, CancellationToken ct = default)
    {
        var query = _db.SubjectDefinitions.AsNoTracking();
        if (activeOnly) query = query.Where(d => d.IsActive);

        return await query
            .OrderBy(d => d.SortOrder)
            .ThenBy(d => d.DisplayName)
            .Select(d => new SubjectDefinitionDto(
                d.Id,
                d.Code,
                d.DisplayName,
                d.Category != null ? d.Category.ToString() : null,
                d.SortOrder,
                d.IsActive,
                d.BatchSubjects.Count,
                d.LibraryUnits.Count))
            .ToListAsync(ct);
    }

    public async Task<SubjectDefinitionDto?> GetAsync(Guid id, CancellationToken ct = default) =>
        await _db.SubjectDefinitions.AsNoTracking()
            .Where(d => d.Id == id)
            .Select(d => new SubjectDefinitionDto(
                d.Id,
                d.Code,
                d.DisplayName,
                d.Category != null ? d.Category.ToString() : null,
                d.SortOrder,
                d.IsActive,
                d.BatchSubjects.Count,
                d.LibraryUnits.Count))
            .FirstOrDefaultAsync(ct);

    public async Task<Result<SubjectDefinitionDto>> CreateAsync(
        CreateSubjectDefinitionRequest req, CancellationToken ct = default)
    {
        var displayName = req.DisplayName.Trim();
        if (string.IsNullOrWhiteSpace(displayName))
            return Result<SubjectDefinitionDto>.Failure("Display name is required.");

        var code = string.IsNullOrWhiteSpace(req.Code)
            ? Slugify(displayName)
            : Slugify(req.Code.Trim());

        if (string.IsNullOrWhiteSpace(code))
            return Result<SubjectDefinitionDto>.Failure("Code is required.");

        if (await _db.SubjectDefinitions.AnyAsync(d => d.Code == code, ct))
            return Result<SubjectDefinitionDto>.Failure($"Code \"{code}\" is already in use.");

        var entity = new SubjectDefinition
        {
            TenantId = _tenant.TenantId,
            Code = code,
            DisplayName = displayName,
            Category = req.Category,
            SortOrder = req.SortOrder,
            IsActive = true
        };

        _db.SubjectDefinitions.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Result<SubjectDefinitionDto>.Success(ToDto(entity, 0, 0));
    }

    public async Task<Result<SubjectDefinitionDto>> UpdateAsync(
        Guid id, UpdateSubjectDefinitionRequest req, CancellationToken ct = default)
    {
        var displayName = req.DisplayName.Trim();
        if (string.IsNullOrWhiteSpace(displayName))
            return Result<SubjectDefinitionDto>.Failure("Display name is required.");

        var entity = await _db.SubjectDefinitions.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (entity is null)
            return Result<SubjectDefinitionDto>.Failure("Subject not found in catalog.");

        entity.DisplayName = displayName;
        entity.SortOrder = req.SortOrder;
        entity.IsActive = req.IsActive;
        entity.Category = req.Category;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        var linked = await _db.Subjects.CountAsync(s => s.SubjectDefinitionId == id, ct);
        var library = await _db.Units.CountAsync(u => u.SubjectDefinitionId == id, ct);
        return Result<SubjectDefinitionDto>.Success(ToDto(entity, linked, library));
    }

    public async Task<Result<SubjectDefinitionDto>> ArchiveAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.SubjectDefinitions.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (entity is null)
            return Result<SubjectDefinitionDto>.Failure("Subject not found in catalog.");

        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var linked = await _db.Subjects.CountAsync(s => s.SubjectDefinitionId == id, ct);
        var library = await _db.Units.CountAsync(u => u.SubjectDefinitionId == id, ct);
        return Result<SubjectDefinitionDto>.Success(ToDto(entity, linked, library));
    }

    public async Task<Result<UnitDto>> CreateLibraryUnitAsync(
        Guid definitionId, CreateLibraryUnitRequest req, CancellationToken ct = default)
    {
        if (!await _db.SubjectDefinitions.AnyAsync(d => d.Id == definitionId && d.IsActive, ct))
            return Result<UnitDto>.Failure("Catalog subject not found or inactive.");

        var unit = new Unit
        {
            TenantId = _tenant.TenantId,
            SubjectDefinitionId = definitionId,
            Title = req.Title.Trim(),
            Order = req.Order
        };

        _db.Units.Add(unit);
        await _db.SaveChangesAsync(ct);
        return Result<UnitDto>.Success(new UnitDto(unit.Id, unit.Title, unit.Order, 0, true));
    }

    public async Task<IReadOnlyList<UnitDto>> ListLibraryUnitsAsync(
        Guid definitionId, CancellationToken ct = default) =>
        await _db.Units.AsNoTracking()
            .Where(u => u.SubjectDefinitionId == definitionId)
            .OrderBy(u => u.Order)
            .Select(u => new UnitDto(u.Id, u.Title, u.Order, u.Topics.Count, true))
            .ToListAsync(ct);

    public async Task<Result> LinkSharedUnitsToSubjectAsync(
        Guid subjectId, LinkSharedUnitsRequest req, CancellationToken ct = default)
    {
        var subject = await _db.Subjects.FirstOrDefaultAsync(s => s.Id == subjectId, ct);
        if (subject is null)
            return Result.Failure("Batch subject not found.");

        if (subject.SubjectDefinitionId is null)
            return Result.Failure("Batch subject is not linked to a catalog entry.");

        var libraryUnits = await _db.Units
            .Where(u => u.SubjectDefinitionId == subject.SubjectDefinitionId)
            .OrderBy(u => u.Order)
            .ToListAsync(ct);

        if (libraryUnits.Count == 0)
            return Result.Failure("No shared library units exist for this catalog subject.");

        var targetIds = req.UnitIds is { Count: > 0 }
            ? req.UnitIds.Distinct().ToList()
            : libraryUnits.Select(u => u.Id).ToList();

        var validIds = libraryUnits.Select(u => u.Id).ToHashSet();
        if (targetIds.Any(id => !validIds.Contains(id)))
            return Result.Failure("One or more library units were not found.");

        var existing = await _db.SubjectSharedUnits
            .Where(l => l.SubjectId == subjectId)
            .ToListAsync(ct);
        _db.SubjectSharedUnits.RemoveRange(existing);

        var order = 1000;
        foreach (var unitId in targetIds)
        {
            _db.SubjectSharedUnits.Add(new SubjectSharedUnit
            {
                TenantId = _tenant.TenantId,
                SubjectId = subjectId,
                UnitId = unitId,
                Order = order++
            });
        }

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static SubjectDefinitionDto ToDto(SubjectDefinition d) =>
        ToDto(d, d.BatchSubjects.Count, d.LibraryUnits.Count);

    private static SubjectDefinitionDto ToDto(SubjectDefinition d, int linked, int library) =>
        new(d.Id, d.Code, d.DisplayName, d.Category?.ToString(), d.SortOrder, d.IsActive, linked, library);

    private static string Slugify(string value)
    {
        var slug = value.Trim().ToLowerInvariant();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-").Trim('-');
        return slug;
    }
}
