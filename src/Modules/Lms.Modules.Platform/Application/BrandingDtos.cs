namespace Lms.Modules.Platform.Application;

public sealed record BrandingDto(
    string Slug,
    string DisplayName,
    string? LogoUrl,
    string? FaviconUrl,
    string PrimaryColor,
    string? SupportEmail,
    string MentorDisplayName,
    bool SyllabusMentorEnabled,
    bool BundlePriceEditEnabled,
    bool McqBulkImportEnabled);

public sealed record UpdateBrandingRequest(
    string DisplayName,
    string? LogoUrl,
    string? FaviconUrl,
    string PrimaryColor,
    string? SupportEmail,
    string? MentorDisplayName);
