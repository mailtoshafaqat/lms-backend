namespace Lms.Modules.Platform.Application;

public interface IPlatformSettingsService
{
    Task<EmailSettingsDto> GetEmailSettingsAsync(CancellationToken ct = default);
    Task<EmailSettingsDto> UpdateEmailSettingsAsync(UpdateEmailSettingsRequest request, CancellationToken ct = default);
    Task<ZoomSettingsDto> GetZoomSettingsAsync(CancellationToken ct = default);
    Task<ZoomSettingsDto> UpdateZoomSettingsAsync(UpdateZoomSettingsRequest request, CancellationToken ct = default);
    Task<BrandingDto?> GetPublicBrandingAsync(string slug, CancellationToken ct = default);
    Task<BrandingDto> GetBrandingAsync(CancellationToken ct = default);
    Task<BrandingDto> UpdateBrandingAsync(UpdateBrandingRequest request, CancellationToken ct = default);
    Task<BrandingDto> UpdateTenantBrandingAsync(Guid tenantId, UpdateBrandingRequest request, CancellationToken ct = default);
}
