using Lms.Modules.QnA.Domain;
using Lms.Modules.QnA.Infrastructure;
using Lms.Shared.Common;
using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.QnA.Application;

public sealed class DoubtTemplateService : IDoubtTemplateService
{
    private readonly QnADbContext _db;
    private readonly ITenantContext _tenant;

    public DoubtTemplateService(QnADbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<IReadOnlyList<DoubtReplyTemplateDto>> ListAsync(CancellationToken ct = default)
    {
        var rows = await _db.DoubtReplyTemplates.AsNoTracking()
            .OrderBy(t => t.Order)
            .ThenBy(t => t.Title)
            .ToListAsync(ct);

        return rows.Select(Map).ToList();
    }

    public async Task<Result<DoubtReplyTemplateDto>> CreateAsync(
        CreateDoubtReplyTemplateRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return Result<DoubtReplyTemplateDto>.Failure("Title is required.");
        if (string.IsNullOrWhiteSpace(request.Body))
            return Result<DoubtReplyTemplateDto>.Failure("Body is required.");

        var maxOrder = await _db.DoubtReplyTemplates.Select(t => (int?)t.Order).MaxAsync(ct) ?? 0;
        var entity = new DoubtReplyTemplate
        {
            TenantId = _tenant.TenantId,
            Title = request.Title.Trim(),
            Body = request.Body.Trim(),
            Order = maxOrder + 1
        };

        _db.DoubtReplyTemplates.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Result<DoubtReplyTemplateDto>.Success(Map(entity));
    }

    public async Task<Result<DoubtReplyTemplateDto>> UpdateAsync(
        Guid id, UpdateDoubtReplyTemplateRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return Result<DoubtReplyTemplateDto>.Failure("Title is required.");
        if (string.IsNullOrWhiteSpace(request.Body))
            return Result<DoubtReplyTemplateDto>.Failure("Body is required.");

        var entity = await _db.DoubtReplyTemplates.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (entity is null) return Result<DoubtReplyTemplateDto>.Failure("Template not found.");

        entity.Title = request.Title.Trim();
        entity.Body = request.Body.Trim();
        await _db.SaveChangesAsync(ct);
        return Result<DoubtReplyTemplateDto>.Success(Map(entity));
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.DoubtReplyTemplates.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (entity is null) return false;
        _db.DoubtReplyTemplates.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static DoubtReplyTemplateDto Map(DoubtReplyTemplate t) =>
        new(t.Id, t.Title, t.Body, t.Order);
}
