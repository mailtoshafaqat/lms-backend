namespace Lms.Shared.Branding;

public sealed record InstituteBrandingSnapshot(string DisplayName, string? LogoUrl);

public interface IInstituteBrandingReader
{
    Task<InstituteBrandingSnapshot?> GetAsync(Guid tenantId, CancellationToken ct = default);
}
