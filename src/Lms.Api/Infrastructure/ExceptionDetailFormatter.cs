namespace Lms.Api.Infrastructure;

internal static class ExceptionDetailFormatter
{
    private const int MaxLength = 4000;

    public static string? Format(Exception? ex)
    {
        if (ex is null) return null;

        var detail = ex.ToString();
        return detail.Length <= MaxLength ? detail : detail[..MaxLength];
    }
}
