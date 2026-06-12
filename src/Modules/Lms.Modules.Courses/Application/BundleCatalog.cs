using Lms.Modules.Courses.Contracts;
using Lms.Modules.Courses.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Courses.Application;

public sealed class BundleCatalog : IBundleCatalog
{
    private readonly CoursesDbContext _db;

    public BundleCatalog(CoursesDbContext db) => _db = db;

    public async Task<BundleSummary?> GetBundleAsync(Guid bundleId, CancellationToken ct = default) =>
        await _db.Bundles
            .Where(b => b.Id == bundleId)
            .Select(b => new BundleSummary(b.Id, b.Title, b.Price, b.ValidityDays, b.IsPublished, b.VideosOnly))
            .FirstOrDefaultAsync(ct);
}
