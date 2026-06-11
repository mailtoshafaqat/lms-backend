using System.Collections;
using System.Reflection;
using System.Text.Json;
using Lms.Modules.Platform.Application;
using Lms.Shared.Auth;
using Lms.Shared.Http;
using Lms.Api.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Lms.Api.Filters;

/// <summary>Adds traceId to API error JSON bodies and stores incidents for support lookup.</summary>
public sealed class ErrorTraceResultFilter : IAsyncAlwaysRunResultFilter
{
    private readonly IRequestIncidentService _incidents;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ErrorTraceResultFilter> _logger;

    public ErrorTraceResultFilter(
        IRequestIncidentService incidents,
        ICurrentUser currentUser,
        ILogger<ErrorTraceResultFilter> logger)
    {
        _incidents = incidents;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        await next();

        if (context.Result is not ObjectResult objectResult)
            return;

        var statusCode = objectResult.StatusCode ?? context.HttpContext.Response.StatusCode;
        if (statusCode < 400)
            return;

        var http = context.HttpContext;
        var traceId = http.GetTraceId();
        objectResult.Value = AttachTraceId(objectResult.Value, traceId);

        var errorMessage = ExtractErrorMessage(objectResult.Value);
        var durationMs = ExceptionHandlingMiddleware.ElapsedMs(http);

        try
        {
            await _incidents.RecordAsync(new RecordRequestIncident(
                traceId,
                http.Request.Method,
                http.Request.Path,
                statusCode,
                errorMessage,
                null,
                null,
                ExceptionHandlingMiddleware.ResolveTenantId(http),
                ExceptionHandlingMiddleware.ResolveTenantSlug(http),
                _currentUser.UserId,
                _currentUser.Email,
                durationMs), http.RequestAborted);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record request incident for trace {TraceId}", traceId);
        }
    }

    private static object AttachTraceId(object? value, string traceId)
    {
        if (value is null)
            return new { error = "Request failed.", traceId };

        if (value is ProblemDetails problem)
        {
            var payload = new Dictionary<string, object?>
            {
                ["error"] = problem.Detail ?? problem.Title ?? "Request failed.",
                ["traceId"] = traceId,
                ["status"] = problem.Status
            };

            if (problem.Extensions.TryGetValue("errors", out var errors))
                payload["errors"] = errors;

            return payload;
        }

        if (value is JsonElement element && element.ValueKind == JsonValueKind.Object)
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(element.GetRawText())
                       ?? new Dictionary<string, object?>();
            dict["traceId"] = traceId;
            if (!dict.ContainsKey("error"))
                dict["error"] = dict.TryGetValue("title", out var title) ? title : "Request failed.";
            return dict;
        }

        if (value is IDictionary dictionary)
        {
            var dict = new Dictionary<string, object?>();
            foreach (DictionaryEntry entry in dictionary)
            {
                if (entry.Key is not null)
                    dict[entry.Key.ToString()!] = entry.Value;
            }

            dict["traceId"] = traceId;
            if (!dict.ContainsKey("error"))
                dict["error"] = dict.TryGetValue("title", out var title) ? title : "Request failed.";
            return dict;
        }

        var error = GetPropertyValue(value, "error")
                    ?? GetPropertyValue(value, "title")
                    ?? "Request failed.";

        return new Dictionary<string, object?>
        {
            ["error"] = error,
            ["traceId"] = traceId
        };
    }

    private static string? ExtractErrorMessage(object? value)
    {
        if (value is null) return null;

        if (value is ProblemDetails problem)
            return problem.Detail ?? problem.Title;

        if (value is IDictionary dictionary)
        {
            if (dictionary.Contains("error"))
                return dictionary["error"]?.ToString();
            if (dictionary.Contains("title"))
                return dictionary["title"]?.ToString();
        }

        return GetPropertyValue(value, "error") ?? GetPropertyValue(value, "title");
    }

    private static string? GetPropertyValue(object value, string propertyName)
    {
        var property = value.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        return property?.GetValue(value)?.ToString();
    }
}
