namespace Lms.Shared.Tenancy;

public static class TenantTrial
{
    public const int DefaultTrialDays = 30;

    public static DateTime DefaultEndsAt(DateTime utcNow) => utcNow.AddDays(DefaultTrialDays);

    public static bool IsExpired(TenantStatus status, DateTime? trialEndsAt, DateTime? utcNow = null)
    {
        if (status != TenantStatus.Trial || trialEndsAt is null) return false;
        return trialEndsAt.Value < (utcNow ?? DateTime.UtcNow);
    }

    public static int? DaysRemaining(DateTime? trialEndsAt, DateTime? utcNow = null)
    {
        if (trialEndsAt is null) return null;
        var days = (int)Math.Ceiling((trialEndsAt.Value - (utcNow ?? DateTime.UtcNow)).TotalDays);
        return Math.Max(0, days);
    }
}
