namespace Lms.Shared.Integrations;

/// <summary>Per-tenant Zoom Server-to-Server OAuth credentials (white-label: each tenant uses its
/// own Zoom account).</summary>
public sealed record TenantZoomSettings(
    bool Enabled,
    string AccountId,
    string ClientId,
    string? ClientSecret);

public interface ITenantZoomSettingsProvider
{
    Task<TenantZoomSettings?> GetAsync(CancellationToken ct = default);
}
