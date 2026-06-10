namespace Lms.Modules.Identity.Application;

public sealed record RegisterRequest(string Email, string Password, string FullName);

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshRequest(string RefreshToken);

public sealed record TenantFeaturesDto(
    Guid TenantId,
    string TenantName,
    string Slug,
    string Status,
    string Plan,
    bool LiveClassesEnabled,
    string ZoomMode,
    string PaymentMode,
    bool AllowStudentSelfEnroll,
    bool AllowAdminCreateStudent,
    bool BundlePriceEditEnabled,
    bool McqBulkImportEnabled);

public sealed record AuthResponse(
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    bool MustChangePassword,
    TenantFeaturesDto? Tenant);

public sealed record UserProfile(Guid UserId, string Email, string FullName, string Role);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResetPasswordRequest(string Token, string NewPassword);
