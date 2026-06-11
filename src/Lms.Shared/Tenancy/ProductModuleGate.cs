namespace Lms.Shared.Tenancy;

public enum ProductModule
{
    MockExams,
    UnitPyqTests,
    MistakeDiary,
    Doubts
}

public static class ProductModuleGate
{
    public static bool IsEnabled(TenantFeatures? features, ProductModule module)
    {
        if (features is null) return false;

        return module switch
        {
            ProductModule.MockExams => features.MockExamsEnabled,
            ProductModule.UnitPyqTests => features.UnitPyqTestsEnabled,
            ProductModule.MistakeDiary => features.MistakeDiaryEnabled,
            ProductModule.Doubts => features.DoubtsEnabled,
            _ => true
        };
    }
}
