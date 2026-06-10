namespace Lms.Modules.Identity.Application;

public sealed record CreateStudentRequest(string FullName, string Email, Guid? BundleId);

public sealed record EnrollStudentRequest(Guid BundleId);

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

public sealed record SetStudentStatusRequest(bool IsActive);

public sealed record ResetStudentPasswordDto(
    Guid UserId,
    string FullName,
    string Email,
    string TempPassword,
    bool EmailSent);

public sealed record CreateTeacherRequest(string FullName, string Email);

public sealed record CreatedTeacherDto(
    Guid UserId,
    string FullName,
    string Email,
    string TempPassword,
    bool EmailSent);

public sealed record TeacherListItemDto(
    Guid UserId,
    string FullName,
    string Email,
    bool IsActive,
    DateTime CreatedAt);

public sealed record SetTeacherStatusRequest(bool IsActive);

public sealed record ResetTeacherPasswordDto(
    Guid UserId,
    string FullName,
    string Email,
    string TempPassword,
    bool EmailSent);

public sealed record StudentGuardianDto(
    Guid Id,
    Guid StudentUserId,
    string Name,
    string Email,
    bool WeeklyReportsEnabled);

public sealed record CreateStudentGuardianRequest(string Name, string Email, bool WeeklyReportsEnabled);

public sealed record UpdateStudentGuardianRequest(string Name, string Email, bool WeeklyReportsEnabled);

public sealed record SendGuardianReportResultDto(bool EmailSent);
