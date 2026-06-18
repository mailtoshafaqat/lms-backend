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

        var rows = await query
            .OrderBy(d => d.SortOrder)
            .ThenBy(d => d.DisplayName)
            .Select(d => new
            {
                d.Id,
                d.Code,
                d.DisplayName,
                Category = d.Category != null ? d.Category.ToString() : null,
                d.SortOrder,
                d.IsActive,
                LinkedBatchCount = d.BatchSubjects.Count,
                LibraryUnitCount = d.LibraryUnits.Count
            })
            .ToListAsync(ct);

        var placements = await LoadPlacementsByDefinitionAsync(rows.Select(r => r.Id).ToList(), ct);

        return rows.Select(r => new SubjectDefinitionDto(
            r.Id,
            r.Code,
            r.DisplayName,
            r.Category,
            r.SortOrder,
            r.IsActive,
            r.LinkedBatchCount,
            r.LibraryUnitCount,
            placements.GetValueOrDefault(r.Id, []))).ToList();
    }

    public async Task<SubjectDefinitionDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var row = await _db.SubjectDefinitions.AsNoTracking()
            .Where(d => d.Id == id)
            .Select(d => new
            {
                d.Id,
                d.Code,
                d.DisplayName,
                Category = d.Category != null ? d.Category.ToString() : null,
                d.SortOrder,
                d.IsActive,
                LinkedBatchCount = d.BatchSubjects.Count,
                LibraryUnitCount = d.LibraryUnits.Count
            })
            .FirstOrDefaultAsync(ct);

        if (row is null) return null;

        var placements = await LoadPlacementsByDefinitionAsync([id], ct);
        return new SubjectDefinitionDto(
            row.Id,
            row.Code,
            row.DisplayName,
            row.Category,
            row.SortOrder,
            row.IsActive,
            row.LinkedBatchCount,
            row.LibraryUnitCount,
            placements.GetValueOrDefault(id, []));
    }

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
        return Result<SubjectDefinitionDto>.Success(ToDto(entity, 0, 0, []));
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

        var linkedSubjects = await _db.Subjects
            .Where(s => s.SubjectDefinitionId == id)
            .ToListAsync(ct);
        foreach (var subject in linkedSubjects)
            subject.Title = displayName;

        await _db.SaveChangesAsync(ct);

        var linked = linkedSubjects.Count;
        var library = await _db.Units.CountAsync(u => u.SubjectDefinitionId == id, ct);
        var placements = await LoadPlacementsByDefinitionAsync([id], ct);
        return Result<SubjectDefinitionDto>.Success(
            ToDto(entity, linked, library, placements.GetValueOrDefault(id, [])));
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
        var placements = await LoadPlacementsByDefinitionAsync([id], ct);
        return Result<SubjectDefinitionDto>.Success(
            ToDto(entity, linked, library, placements.GetValueOrDefault(id, [])));
    }

    public async Task<Result<UnitDto>> CreateLibraryUnitAsync(
        Guid definitionId, CreateLibraryUnitRequest req, CancellationToken ct = default)
    {
        if (!await _db.SubjectDefinitions.AnyAsync(d => d.Id == definitionId && d.IsActive, ct))
            return Result<UnitDto>.Failure("Catalog subject not found or inactive.");

        var title = req.Title.Trim();
        if (string.IsNullOrWhiteSpace(title))
            return Result<UnitDto>.Failure("Unit title is required.");

        if (await HasDuplicateLibraryTitleAsync(definitionId, title, null, ct))
            return Result<UnitDto>.Failure($"A shared unit named \"{title}\" already exists for this subject.");

        var unit = new Unit
        {
            TenantId = _tenant.TenantId,
            SubjectDefinitionId = definitionId,
            Title = title,
            Order = req.Order
        };

        _db.Units.Add(unit);
        await _db.SaveChangesAsync(ct);
        return Result<UnitDto>.Success(new UnitDto(unit.Id, unit.Title, unit.Order, 0, true));
    }

    public async Task<Result<UnitDto>> UpdateLibraryUnitAsync(
        Guid definitionId, Guid unitId, UpdateLibraryUnitRequest req, CancellationToken ct = default)
    {
        var title = req.Title.Trim();
        if (string.IsNullOrWhiteSpace(title))
            return Result<UnitDto>.Failure("Unit title is required.");

        var unit = await _db.Units.FirstOrDefaultAsync(
            u => u.Id == unitId && u.SubjectDefinitionId == definitionId, ct);
        if (unit is null)
            return Result<UnitDto>.Failure("Library unit not found.");

        if (await HasDuplicateLibraryTitleAsync(definitionId, title, unitId, ct))
            return Result<UnitDto>.Failure($"A shared unit named \"{title}\" already exists for this subject.");

        unit.Title = title;
        unit.Order = req.Order;
        unit.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var topicCount = await _db.Topics.CountAsync(t => t.UnitId == unitId, ct);
        return Result<UnitDto>.Success(new UnitDto(unit.Id, unit.Title, unit.Order, topicCount, true));
    }

    public async Task<Result> DeleteLibraryUnitAsync(
        Guid definitionId, Guid unitId, CancellationToken ct = default)
    {
        var unit = await _db.Units.FirstOrDefaultAsync(
            u => u.Id == unitId && u.SubjectDefinitionId == definitionId, ct);
        if (unit is null)
            return Result.Failure("Library unit not found.");

        var topicCount = await _db.Topics.CountAsync(t => t.UnitId == unitId, ct);
        if (topicCount > 0)
            return Result.Failure(
                $"This shared unit has {topicCount} topic(s). Delete topics from the Content page first, then remove the unit.");

        var links = await _db.SubjectSharedUnits.Where(l => l.UnitId == unitId).ToListAsync(ct);
        _db.SubjectSharedUnits.RemoveRange(links);
        _db.Units.Remove(unit);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
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

    private async Task<Dictionary<Guid, IReadOnlyList<LinkedBatchPlacementDto>>> LoadPlacementsByDefinitionAsync(
        IReadOnlyList<Guid> definitionIds, CancellationToken ct)
    {
        if (definitionIds.Count == 0)
            return new Dictionary<Guid, IReadOnlyList<LinkedBatchPlacementDto>>();

        var rows = await (
            from s in _db.Subjects.AsNoTracking()
            join b in _db.Bundles.AsNoTracking() on s.BundleId equals b.Id
            where s.SubjectDefinitionId != null && definitionIds.Contains(s.SubjectDefinitionId.Value)
            orderby b.Title
            select new
            {
                DefinitionId = s.SubjectDefinitionId!.Value,
                Placement = new LinkedBatchPlacementDto(b.Id, b.Title, s.Id)
            }).ToListAsync(ct);

        return rows
            .GroupBy(r => r.DefinitionId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<LinkedBatchPlacementDto>)g.Select(x => x.Placement).ToList());
    }

    private Task<bool> HasDuplicateLibraryTitleAsync(
        Guid definitionId, string title, Guid? excludeUnitId, CancellationToken ct)
    {
        var normalized = title.Trim().ToLower();
        var query = _db.Units.Where(u =>
            u.SubjectDefinitionId == definitionId &&
            u.Title.ToLower() == normalized);
        if (excludeUnitId is Guid exclude)
            query = query.Where(u => u.Id != exclude);
        return query.AnyAsync(ct);
    }

    private static SubjectDefinitionDto ToDto(
        SubjectDefinition d,
        int linked,
        int library,
        IReadOnlyList<LinkedBatchPlacementDto> placements) =>
        new(d.Id, d.Code, d.DisplayName, d.Category?.ToString(), d.SortOrder, d.IsActive, linked, library, placements);

    private static string Slugify(string value)
    {
        var slug = value.Trim().ToLowerInvariant();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-").Trim('-');
        return slug;
    }
}
