namespace Lms.Shared.Configuration;

/// <summary>Public URLs for links, redirects, and payment callbacks. Set per environment in appsettings.</summary>
public sealed class AppUrlOptions
{
    public const string SectionName = "App";

    /// <summary>Student/admin frontend origin (e.g. https://learn.academy.com).</summary>
    public string BaseUrl { get; set; } = "";

    /// <summary>Public API origin for webhooks (e.g. https://api.academy.com). Falls back to BaseUrl when unset.</summary>
    public string? ApiBaseUrl { get; set; }
}

public sealed class PaymentsOptions
{
    public const string SectionName = "Payments";

    public bool JazzCashSandbox { get; set; } = true;
    public bool EasypaisaSandbox { get; set; } = true;
}

public interface IAppUrls
{
    string FrontendBaseUrl { get; }
    string ApiBaseUrl { get; }
}

public sealed class AppUrls : IAppUrls
{
    public string FrontendBaseUrl { get; }
    public string ApiBaseUrl { get; }

    public AppUrls(Microsoft.Extensions.Options.IOptions<AppUrlOptions> options)
    {
        var o = options.Value;
        if (string.IsNullOrWhiteSpace(o.BaseUrl))
            throw new InvalidOperationException(
                $"Configure {AppUrlOptions.SectionName}:BaseUrl in appsettings (student/admin site URL).");

        FrontendBaseUrl = o.BaseUrl.TrimEnd('/');
        ApiBaseUrl = (string.IsNullOrWhiteSpace(o.ApiBaseUrl) ? o.BaseUrl : o.ApiBaseUrl).TrimEnd('/');
    }
}
