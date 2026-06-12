using Lms.Modules.Courses.Infrastructure;
using Lms.Shared.Courses;
using Lms.Shared.Tenancy;

namespace Lms.Modules.Courses.Application;

public sealed class SubjectCatalogProvisioner : ISubjectCatalogProvisioner
{
    private readonly CoursesDbContext _db;

    public SubjectCatalogProvisioner(CoursesDbContext db) => _db = db;

    public Task EnsureTemplateForTenantAsync(
        Guid tenantId, ProductProfile profile, CancellationToken ct = default) =>
        SubjectCatalogSeeder.SeedForTenantAsync(_db, tenantId, profile, ct);
}
