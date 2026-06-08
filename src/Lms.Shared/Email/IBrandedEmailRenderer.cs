namespace Lms.Shared.Email;

/// <summary>Wraps transactional email bodies in per-tenant branding (logo, colors, institute name).</summary>
public interface IBrandedEmailRenderer
{
    Task<string> RenderAsync(Guid tenantId, string subject, string bodyHtml, CancellationToken ct = default);
}
