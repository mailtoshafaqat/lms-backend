using Lms.Modules.Courses.Domain;
using Lms.Modules.Courses.Infrastructure;
using Lms.Shared.Common;
using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Courses.Application;

public sealed class CourseAdminService : ICourseAdminService
{
    private readonly CoursesDbContext _db;
    private readonly ITenantContext _tenant;

    public CourseAdminService(CoursesDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<BundleDto> CreateBundleAsync(CreateBundleRequest req, CancellationToken ct = default)
    {
        var bundle = new Bundle
        {
            TenantId = _tenant.TenantId,
            Title = req.Title.Trim(),
            Price = req.Price,
            ValidityDays = req.ValidityDays <= 0 ? 365 : req.ValidityDays,
            IsPublished = true,
            VideosOnly = req.VideosOnly
        };
        _db.Bundles.Add(bundle);
        await _db.SaveChangesAsync(ct);
        return new BundleDto(bundle.Id, bundle.Title, 0, bundle.Price, bundle.VideosOnly);
    }

    public async Task<Result<BundleDto>> UpdateBundleAsync(
        Guid bundleId, UpdateBundleRequest req, CancellationToken ct = default)
    {
        var bundle = await _db.Bundles.FirstOrDefaultAsync(b => b.Id == bundleId, ct);
        if (bundle is null)
            return Result<BundleDto>.Failure("Bundle not found.");
        if (req.Price < 0)
            return Result<BundleDto>.Failure("Price cannot be negative.");

        bundle.Price = req.Price;
        if (req.ValidityDays is > 0)
            bundle.ValidityDays = req.ValidityDays.Value;
        if (req.VideosOnly is not null)
            bundle.VideosOnly = req.VideosOnly.Value;

        await _db.SaveChangesAsync(ct);
        var subjectCount = await _db.Subjects.CountAsync(s => s.BundleId == bundleId, ct);
        return Result<BundleDto>.Success(
            new BundleDto(bundle.Id, bundle.Title, subjectCount, bundle.Price, bundle.VideosOnly));
    }

    public async Task<Result<SubjectDto>> CreateSubjectAsync(Guid bundleId, CreateSubjectRequest req, CancellationToken ct = default)
    {
        if (!await _db.Bundles.AnyAsync(b => b.Id == bundleId, ct))
            return Result<SubjectDto>.Failure("Bundle not found.");

        if (req.SubjectDefinitionId is not Guid definitionId)
            return Result<SubjectDto>.Failure(
                "Pick a subject from the institute catalog. Add new subjects under Subject catalog first.");

        var definition = await _db.SubjectDefinitions
            .FirstOrDefaultAsync(d => d.Id == definitionId && d.IsActive, ct);
        if (definition is null)
            return Result<SubjectDto>.Failure("Catalog subject not found or inactive.");

        if (await _db.Subjects.AnyAsync(
                s => s.BundleId == bundleId && s.SubjectDefinitionId == definitionId, ct))
            return Result<SubjectDto>.Failure(
                $"\"{definition.DisplayName}\" is already in this batch.");

        var title = definition.DisplayName;

        var subject = new Subject
        {
            TenantId = _tenant.TenantId,
            BundleId = bundleId,
            SubjectDefinitionId = definition?.Id,
            Title = title,
            Order = req.Order
        };
        _db.Subjects.Add(subject);
        await _db.SaveChangesAsync(ct);

        if (req.IncludeSharedContent && definition is not null)
        {
            var libraryUnitIds = await _db.Units
                .Where(u => u.SubjectDefinitionId == definition.Id)
                .OrderBy(u => u.Order)
                .Select(u => u.Id)
                .ToListAsync(ct);

            var order = 1000;
            foreach (var unitId in libraryUnitIds)
            {
                _db.SubjectSharedUnits.Add(new Domain.SubjectSharedUnit
                {
                    TenantId = _tenant.TenantId,
                    SubjectId = subject.Id,
                    UnitId = unitId,
                    Order = order++
                });
            }

            if (libraryUnitIds.Count > 0)
                await _db.SaveChangesAsync(ct);
        }

        return Result<SubjectDto>.Success(await ToSubjectDtoAsync(subject.Id, ct));
    }

    public async Task<Result<UnitDto>> CreateUnitAsync(Guid subjectId, CreateUnitRequest req, CancellationToken ct = default)
    {
        if (!await _db.Subjects.AnyAsync(s => s.Id == subjectId, ct))
            return Result<UnitDto>.Failure("Subject not found.");

        var unit = new Unit
        {
            TenantId = _tenant.TenantId,
            SubjectId = subjectId,
            Title = req.Title.Trim(),
            Order = req.Order
        };
        _db.Units.Add(unit);
        await _db.SaveChangesAsync(ct);
        return Result<UnitDto>.Success(new UnitDto(unit.Id, unit.Title, unit.Order, 0));
    }

    public async Task<Result<TopicDto>> CreateTopicAsync(Guid unitId, CreateTopicRequest req, CancellationToken ct = default)
    {
        if (!await _db.Units.AnyAsync(u => u.Id == unitId, ct))
            return Result<TopicDto>.Failure("Unit not found.");

        var topic = new Topic
        {
            TenantId = _tenant.TenantId,
            UnitId = unitId,
            Title = req.Title.Trim(),
            Order = req.Order,
            HasVideo = req.HasVideo
        };
        _db.Topics.Add(topic);
        await _db.SaveChangesAsync(ct);
        return Result<TopicDto>.Success(ToTopicDto(topic));
    }

    public async Task<Result<TopicDto>> GetTopicAsync(Guid topicId, CancellationToken ct = default)
    {
        var topic = await _db.Topics.AsNoTracking().FirstOrDefaultAsync(t => t.Id == topicId, ct);
        return topic is null
            ? Result<TopicDto>.Failure("Topic not found.")
            : Result<TopicDto>.Success(ToTopicDto(topic));
    }

    public async Task<Result<SubjectDto>> UpdateSubjectAsync(
        Guid subjectId, UpdateSubjectRequest req, CancellationToken ct = default)
    {
        var title = req.Title.Trim();
        if (string.IsNullOrWhiteSpace(title))
            return Result<SubjectDto>.Failure("Subject title is required.");

        var subject = await _db.Subjects.FirstOrDefaultAsync(s => s.Id == subjectId, ct);
        if (subject is null)
            return Result<SubjectDto>.Failure("Subject not found.");
        if (subject.SubjectDefinitionId is not null)
            return Result<SubjectDto>.Failure(
                "Catalog-linked subjects cannot be renamed here. Edit the name in Subject catalog.");

        subject.Title = title;
        await _db.SaveChangesAsync(ct);
        var unitCount = await _db.Units.CountAsync(u => u.SubjectId == subjectId, ct);
        return Result<SubjectDto>.Success(await ToSubjectDtoAsync(subject.Id, ct));
    }

    public async Task<Result<UnitDto>> UpdateUnitAsync(Guid unitId, UpdateUnitRequest req, CancellationToken ct = default)
    {
        var title = req.Title.Trim();
        if (string.IsNullOrWhiteSpace(title))
            return Result<UnitDto>.Failure("Unit title is required.");

        var unit = await _db.Units.FirstOrDefaultAsync(u => u.Id == unitId, ct);
        if (unit is null)
            return Result<UnitDto>.Failure("Unit not found.");

        unit.Title = title;
        await _db.SaveChangesAsync(ct);
        var topicCount = await _db.Topics.CountAsync(t => t.UnitId == unitId, ct);
        return Result<UnitDto>.Success(new UnitDto(unit.Id, unit.Title, unit.Order, topicCount));
    }

    public async Task<Result<TopicDto>> UpdateTopicAsync(Guid topicId, UpdateTopicRequest req, CancellationToken ct = default)
    {
        var title = req.Title.Trim();
        if (string.IsNullOrWhiteSpace(title))
            return Result<TopicDto>.Failure("Topic title is required.");

        var topic = await _db.Topics.FirstOrDefaultAsync(t => t.Id == topicId, ct);
        if (topic is null)
            return Result<TopicDto>.Failure("Topic not found.");

        topic.Title = title;
        await _db.SaveChangesAsync(ct);
        return Result<TopicDto>.Success(ToTopicDto(topic));
    }

    private static TopicDto ToTopicDto(Topic topic) =>
        new(topic.Id, topic.Title, topic.Order, topic.HasVideo, topic.McqCount, topic.FlashcardCount);

    private async Task<SubjectDto> ToSubjectDtoAsync(Guid subjectId, CancellationToken ct)
    {
        var subject = await _db.Subjects.AsNoTracking().FirstAsync(s => s.Id == subjectId, ct);
        var ownCount = await _db.Units.CountAsync(u => u.SubjectId == subjectId, ct);
        var sharedCount = await _db.SubjectSharedUnits.CountAsync(l => l.SubjectId == subjectId, ct);
        return new SubjectDto(
            subject.Id,
            subject.Title,
            subject.Order,
            ownCount + sharedCount,
            subject.SubjectDefinitionId,
            subject.SubjectDefinitionId is not null,
            sharedCount);
    }

    public async Task<bool> DeleteBundleAsync(Guid id, CancellationToken ct = default) =>
        await DeleteAsync(_db.Bundles, id, ct);

    public async Task<bool> DeleteSubjectAsync(Guid id, CancellationToken ct = default) =>
        await DeleteAsync(_db.Subjects, id, ct);

    public async Task<bool> DeleteUnitAsync(Guid id, CancellationToken ct = default) =>
        await DeleteAsync(_db.Units, id, ct);

    public async Task<bool> DeleteTopicAsync(Guid id, CancellationToken ct = default) =>
        await DeleteAsync(_db.Topics, id, ct);

    private async Task<bool> DeleteAsync<T>(DbSet<T> set, Guid id, CancellationToken ct) where T : class
    {
        var entity = await set.FindAsync([id], ct);
        if (entity is null) return false;
        set.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
