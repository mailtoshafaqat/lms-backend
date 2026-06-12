using Lms.Shared.Tenancy;

namespace Lms.Shared.Courses;

/// <summary>Seeds the exam-prep subject catalog when a tenant is created or profile changes.</summary>
public interface ISubjectCatalogProvisioner
{
    Task EnsureTemplateForTenantAsync(
        Guid tenantId, ProductProfile profile, CancellationToken ct = default);
}
