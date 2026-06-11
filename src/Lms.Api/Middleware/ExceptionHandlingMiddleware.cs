using Lms.Api.Infrastructure;
using Lms.Modules.Platform.Application;
using Lms.Shared.Auth;
using Lms.Shared.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lms.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IRequestIncidentService incidents,
        ICurrentUser currentUser)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
                throw;

            var traceId = context.GetTraceId();
            var durationMs = ElapsedMs(context);

            _logger.LogError(ex, "Unhandled exception {TraceId} {Method} {Path}", traceId, context.Request.Method, context.Request.Path);

            await incidents.RecordAsync(new RecordRequestIncident(
                traceId,
                context.Request.Method,
                context.Request.Path,
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.",
                ex.GetType().Name,
                ExceptionDetailFormatter.Format(ex),
                ResolveTenantId(context),
                ResolveTenantSlug(context),
                currentUser.UserId,
                currentUser.Email,
                durationMs), context.RequestAborted);

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "An unexpected error occurred.",
                traceId
            });
        }
    }

    internal static int ElapsedMs(HttpContext context)
    {
        if (context.Items.TryGetValue(TraceIdExtensions.DurationItemKey, out var started)
            && started is DateTime start)
        {
            return (int)Math.Max(0, (DateTime.UtcNow - start).TotalMilliseconds);
        }

        return 0;
    }

    internal static Guid? ResolveTenantId(HttpContext context) =>
        context.Items.TryGetValue(TenantResolutionMiddleware.TenantIdItemKey, out var id) && id is Guid tenantId
            ? tenantId
            : null;

    internal static string? ResolveTenantSlug(HttpContext context) =>
        context.Items.TryGetValue(TenantResolutionMiddleware.TenantSlugItemKey, out var slug)
            ? slug?.ToString()
            : null;
}
