namespace Lms.Modules.Identity.Application;

public sealed record CreateStudentRequest(string FullName, string Email, Guid? BundleId);

/// <summary>Returned to the admin after provisioning. <see cref="TempPassword"/> is shown once so the
/// admin can relay it if email delivery is not yet configured.</summary>
public sealed record CreatedStudentDto(
    Guid UserId,
    string FullName,
    string Email,
    string TempPassword,
    bool EmailSent,
    string? BundleTitle,
    DateTime? ExpiresAt);

public sealed record StudentListItemDto(
    Guid UserId,
    string FullName,
    string Email,
    bool IsActive,
    DateTime CreatedAt);
