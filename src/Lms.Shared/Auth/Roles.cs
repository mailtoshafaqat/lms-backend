namespace Lms.Shared.Auth;

/// <summary>Platform roles. Used for authorization policies across modules.</summary>
public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string InstituteAdmin = "InstituteAdmin";
    public const string Teacher = "Teacher";
    public const string Student = "Student";
    public const string Support = "Support";
}
