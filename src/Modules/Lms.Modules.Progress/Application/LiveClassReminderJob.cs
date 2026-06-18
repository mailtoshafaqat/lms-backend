using Lms.Shared.Enrollments;
using Lms.Shared.LiveClasses;
using Lms.Shared.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lms.Modules.Progress.Application;

/// <summary>Notifies enrolled students ~30 minutes before a live class starts.</summary>
public sealed class LiveClassReminderJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<LiveClassReminderJob> _logger;

    public LiveClassReminderJob(IServiceScopeFactory scopes, ILogger<LiveClassReminderJob> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunPassAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Live class reminder job failed.");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    private async Task RunPassAsync(CancellationToken ct)
    {
        using var scope = _scopes.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<ILiveClassReminderReader>();
        var enrollments = scope.ServiceProvider.GetRequiredService<IEnrollmentReader>();
        var notifications = scope.ServiceProvider.GetRequiredService<IStudentNotificationService>();

        var now = DateTime.UtcNow;
        var windowStart = now.AddMinutes(25);
        var windowEnd = now.AddMinutes(35);

        var classes = await reader.GetPendingRemindersAsync(windowStart, windowEnd, ct);
        if (classes.Count == 0) return;

        _logger.LogInformation("Live class reminders: {Count} classes in window.", classes.Count);

        foreach (var liveClass in classes)
        {
            var studentIds = await enrollments.GetActiveUserIdsForBundleAsync(liveClass.BundleId, ct);
            if (studentIds.Count == 0)
            {
                await reader.MarkReminderSentAsync(liveClass.Id, ct);
                continue;
            }

            var startLocal = liveClass.ScheduledStartUtc.ToString("HH:mm 'UTC'");
            var body =
                $"Your live class {liveClass.Title} ({liveClass.SubjectTitle}) starts at {startLocal}.";

            var requests = studentIds.Select(id => new CreateStudentNotificationRequest(
                liveClass.TenantId,
                id,
                "Live class starting soon",
                body,
                "/dashboard",
                SendEmail: true,
                EmailSubject: $"Live class soon: {liveClass.Title}")).ToList();

            await notifications.NotifyManyAsync(requests, ct);
            await reader.MarkReminderSentAsync(liveClass.Id, ct);
        }
    }
}
