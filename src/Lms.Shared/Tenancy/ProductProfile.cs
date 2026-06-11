namespace Lms.Shared.Tenancy;

/// <summary>Institute product vertical — drives default module visibility (menus + API guards).</summary>
public enum ProductProfile
{
    ExamPrep = 0,
    GeneralLms = 1,
    Both = 2
}

/// <summary>Exam-prep modules derived from <see cref="ProductProfile"/> (not stored per module).</summary>
public static class ProductProfileModules
{
    public static bool MockExamsEnabled(ProductProfile profile) =>
        profile is ProductProfile.ExamPrep or ProductProfile.Both;

    public static bool UnitPyqTestsEnabled(ProductProfile profile) =>
        profile is ProductProfile.ExamPrep or ProductProfile.Both;

    public static bool MistakeDiaryEnabled(ProductProfile profile) =>
        profile is ProductProfile.ExamPrep or ProductProfile.Both;

    public static bool DoubtsEnabled(ProductProfile profile) =>
        profile is ProductProfile.ExamPrep or ProductProfile.Both;

    public static bool SyllabusMentorAllowed(ProductProfile profile) =>
        profile is ProductProfile.ExamPrep or ProductProfile.Both;
}

/// <summary>Default tenant flags when a profile is chosen at institute creation.</summary>
public static class ProductProfileDefaults
{
    public static void Apply(TenantProfileSeed target, ProductProfile profile)
    {
        switch (profile)
        {
            case ProductProfile.GeneralLms:
                target.LiveClassesEnabled = false;
                target.SyllabusMentorEnabled = false;
                target.McqBulkImportEnabled = false;
                target.AllowStudentSelfEnroll = true;
                break;
            case ProductProfile.Both:
                target.LiveClassesEnabled = true;
                target.SyllabusMentorEnabled = true;
                target.McqBulkImportEnabled = true;
                target.AllowStudentSelfEnroll = false;
                break;
            default:
                target.LiveClassesEnabled = true;
                target.SyllabusMentorEnabled = true;
                target.McqBulkImportEnabled = true;
                target.AllowStudentSelfEnroll = false;
                break;
        }
    }
}

public sealed class TenantProfileSeed
{
    public bool LiveClassesEnabled { get; set; }
    public bool SyllabusMentorEnabled { get; set; }
    public bool McqBulkImportEnabled { get; set; }
    public bool AllowStudentSelfEnroll { get; set; }
}
