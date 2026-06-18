namespace Lms.Modules.Platform.Application;

public sealed record EnrollmentSettingsDto(bool AllowStudentSelfEnroll);

public sealed record UpdateEnrollmentSettingsRequest(bool AllowStudentSelfEnroll);
