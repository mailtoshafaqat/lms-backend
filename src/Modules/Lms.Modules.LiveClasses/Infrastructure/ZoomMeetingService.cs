using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Lms.Shared.Integrations;
using Microsoft.Extensions.Logging;

namespace Lms.Modules.LiveClasses.Infrastructure;

public sealed record ZoomMeeting(string MeetingId, string JoinUrl, string StartUrl, string? Passcode);

public interface IZoomMeetingService
{
    /// <summary>True when the current tenant has Zoom enabled and credentials configured.</summary>
    Task<bool> IsConfiguredAsync(CancellationToken ct = default);

    /// <summary>Creates a scheduled Zoom meeting on the tenant's account, or returns null when Zoom
    /// is not configured (caller should fall back to a manual link). Throws on API failure.</summary>
    Task<ZoomMeeting?> CreateMeetingAsync(string topic, DateTime startUtc, int durationMinutes, CancellationToken ct = default);
}

public sealed class ZoomMeetingService : IZoomMeetingService
{
    private readonly HttpClient _http;
    private readonly ITenantZoomSettingsProvider _settings;
    private readonly ILogger<ZoomMeetingService> _logger;

    public ZoomMeetingService(
        HttpClient http, ITenantZoomSettingsProvider settings, ILogger<ZoomMeetingService> logger)
    {
        _http = http;
        _settings = settings;
        _logger = logger;
    }

    public async Task<bool> IsConfiguredAsync(CancellationToken ct = default)
    {
        var s = await _settings.GetAsync(ct);
        return s is not null && s.Enabled
            && !string.IsNullOrWhiteSpace(s.AccountId)
            && !string.IsNullOrWhiteSpace(s.ClientId)
            && !string.IsNullOrWhiteSpace(s.ClientSecret);
    }

    public async Task<ZoomMeeting?> CreateMeetingAsync(
        string topic, DateTime startUtc, int durationMinutes, CancellationToken ct = default)
    {
        var s = await _settings.GetAsync(ct);
        if (s is null || !s.Enabled
            || string.IsNullOrWhiteSpace(s.AccountId)
            || string.IsNullOrWhiteSpace(s.ClientId)
            || string.IsNullOrWhiteSpace(s.ClientSecret))
        {
            return null;
        }

        var accessToken = await GetAccessTokenAsync(s.AccountId, s.ClientId, s.ClientSecret!, ct);

        var body = new
        {
            topic,
            type = 2, // scheduled
            start_time = startUtc.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            duration = durationMinutes,
            timezone = "UTC",
            settings = new { join_before_host = true, waiting_room = false, approval_type = 2 }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.zoom.us/v2/users/me/meetings")
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var res = await _http.SendAsync(req, ct);
        var json = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode)
        {
            _logger.LogError("Zoom create meeting failed: {Status} {Body}", res.StatusCode, json);
            throw new InvalidOperationException($"Zoom rejected the request ({(int)res.StatusCode}).");
        }

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        return new ZoomMeeting(
            root.GetProperty("id").ToString(),
            root.GetProperty("join_url").GetString() ?? string.Empty,
            root.TryGetProperty("start_url", out var su) ? su.GetString() ?? string.Empty : string.Empty,
            root.TryGetProperty("password", out var pw) ? pw.GetString() : null);
    }

    private async Task<string> GetAccessTokenAsync(
        string accountId, string clientId, string clientSecret, CancellationToken ct)
    {
        var url = $"https://zoom.us/oauth/token?grant_type=account_credentials&account_id={Uri.EscapeDataString(accountId)}";
        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);

        using var res = await _http.SendAsync(req, ct);
        var json = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode)
        {
            _logger.LogError("Zoom token request failed: {Status} {Body}", res.StatusCode, json);
            throw new InvalidOperationException("Could not authenticate with Zoom. Check the credentials.");
        }

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("Zoom did not return an access token.");
    }
}
