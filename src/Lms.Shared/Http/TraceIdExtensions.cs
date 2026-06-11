using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Lms.Shared.Http;

public static class TraceIdExtensions
{
    public const string ItemKey = "TraceId";
    public const string HeaderName = "X-Trace-Id";
    public const string DurationItemKey = "RequestStartUtc";

    public static string GetTraceId(this HttpContext context)
    {
        if (context.Items.TryGetValue(ItemKey, out var value) && value is string traceId && !string.IsNullOrWhiteSpace(traceId))
            return traceId;

        return Activity.Current?.Id ?? context.TraceIdentifier;
    }
}
