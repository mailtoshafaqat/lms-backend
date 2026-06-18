using Lms.Shared.Entities;
using Lms.Shared.Payments;

namespace Lms.Modules.Platform.Domain;

/// <summary>Per-tenant platform configuration. Currently email/SMTP; will hold branding later.
/// One row per tenant.</summary>
public sealed class TenantSettings : TenantEntity
{
    public bool EmailEnabled { get; set; }
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SmtpUser { get; set; } = string.Empty;
    public string? SmtpPassword { get; set; }
    public bool UseSsl { get; set; } = true;

    // Zoom Server-to-Server OAuth (per-tenant, for live classes).
    public bool ZoomEnabled { get; set; }
    public string ZoomAccountId { get; set; } = string.Empty;
    public string ZoomClientId { get; set; } = string.Empty;
    public string? ZoomClientSecret { get; set; }

    // White-label branding (institute admin configures; shown on login, dashboard header).
    public string DisplayName { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string PrimaryColor { get; set; } = "#0b3d91";
    public string? SupportEmail { get; set; }
    public string? FaviconUrl { get; set; }

    /// <summary>UI label for Syllabus Mentor (e.g. "Demo Academy Mentor"). Falls back to "{DisplayName} Mentor".</summary>
    public string? MentorDisplayName { get; set; }

    // Tenant-managed payments (institute merchant accounts).
    public string? ManualPaymentInstructions { get; set; }
    public bool ManualPaymentEnabled { get; set; }
    public bool StripeEnabled { get; set; }
    public string? StripePublishableKey { get; set; }
    public string? StripeSecretKey { get; set; }
    public string? StripeWebhookSecret { get; set; }
    public bool JazzCashEnabled { get; set; }
    public string? JazzCashMerchantId { get; set; }
    public string? JazzCashPassword { get; set; }
    public string? JazzCashHashKey { get; set; }
    public string? JazzCashReturnUrl { get; set; }
    public bool EasypaisaEnabled { get; set; }
    public string? EasypaisaStoreId { get; set; }
    public string? EasypaisaHashKey { get; set; }
    public string? EasypaisaCredentials { get; set; }
}
