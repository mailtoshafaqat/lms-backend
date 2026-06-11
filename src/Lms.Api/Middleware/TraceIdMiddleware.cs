using System.Diagnostics;
using Lms.Shared.Http;

namespace Lms.Api.Middleware;

/// <summary>Assigns a stable trace id per request and echoes it on the response.</summary>
public sealed class TraceIdMiddleware
{
    private readonly RequestDelegate _next;

    public TraceIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        context.Items[TraceIdExtensions.ItemKey] = traceId;
        context.Items[TraceIdExtensions.DurationItemKey] = DateTime.UtcNow;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[TraceIdExtensions.HeaderName] = traceId;
            return Task.CompletedTask;
        });

        await _next(context);
    }
}
