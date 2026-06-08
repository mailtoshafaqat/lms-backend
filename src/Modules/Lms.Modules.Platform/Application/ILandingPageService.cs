namespace Lms.Modules.Platform.Application;

public interface ILandingPageService
{
    Task<LandingPageDto?> GetPublicAsync(string slug, CancellationToken ct = default);
    Task<LandingPageDto> GetAdminAsync(CancellationToken ct = default);
    Task<LandingPageDto> UpdateAdminAsync(UpdateLandingPageRequest request, CancellationToken ct = default);
}
