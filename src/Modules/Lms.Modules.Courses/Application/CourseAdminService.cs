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
            IsPublished = true
        };
        _db.Bundles.Add(bundle);
        await _db.SaveChangesAsync(ct);
        return new BundleDto(bundle.Id, bundle.Title, 0, bundle.Price);
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

        await _db.SaveChangesAsync(ct);
        var subjectCount = await _db.Subjects.CountAsync(s => s.BundleId == bundleId, ct);
        return Result<BundleDto>.Success(new BundleDto(bundle.Id, bundle.Title, subjectCount, bundle.Price));
    }

    public async Task<Result<SubjectDto>> CreateSubjectAsync(Guid bundleId, CreateSubjectRequest req, CancellationToken ct = default)
    {
        if (!await _db.Bundles.AnyAsync(b => b.Id == bundleId, ct))
            return Result<SubjectDto>.Failure("Bundle not found.");

        var subject = new Subject
        {
            TenantId = _tenant.TenantId,
            BundleId = bundleId,
            Title = req.Title.Trim(),
            Order = req.Order
        };
        _db.Subjects.Add(subject);
        await _db.SaveChangesAsync(ct);
        return Result<SubjectDto>.Success(new SubjectDto(subject.Id, subject.Title, subject.Order, 0));
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
        return Result<TopicDto>.Success(
            new TopicDto(topic.Id, topic.Title, topic.Order, topic.HasVideo, topic.McqCount, topic.FlashcardCount));
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
