using Lms.Modules.Identity.Infrastructure;
using Lms.Shared.Email;
using Lms.Shared.Progress;
using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lms.Modules.Identity.Application;

/// <summary>Stub hosted job for weekly guardian progress emails. Logs eligible guardians; full scheduling can be wired later.</summary>
public sealed class GuardianWeeklyReportJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<GuardianWeeklyReportJob> _logger;

    public GuardianWeeklyReportJob(IServiceScopeFactory scopes, ILogger<GuardianWeeklyReportJob> logger)
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
                await RunWeeklyPassAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Weekly guardian report job failed.");
            }

            await Task.Delay(TimeSpan.FromDays(7), stoppingToken);
        }
    }

    private async Task RunWeeklyPassAsync(CancellationToken ct)
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var gradesReader = scope.ServiceProvider.GetRequiredService<IStudentGradesReader>();
        var email = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var branded = scope.ServiceProvider.GetRequiredService<IBrandedEmailRenderer>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        var guardians = await db.StudentGuardians.AsNoTracking()
            .Where(g => g.WeeklyReportsEnabled)
            .ToListAsync(ct);

        _logger.LogInformation("Weekly guardian report stub: {Count} guardians eligible.", guardians.Count);

        foreach (var guardian in guardians)
        {
            var student = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == guardian.StudentUserId, ct);
            if (student is null) continue;

            var grades = await gradesReader.GetRecentGradesAsync(student.Id, ct: ct);
            var body = BuildReportBody(student.FullName, grades);
            var html = await branded.RenderAsync(guardian.TenantId, "Weekly progress report", body, ct);

            try
            {
                await email.SendForTenantAsync(
                    guardian.TenantId,
                    new EmailMessage(guardian.Email, guardian.Name, $"Progress report: {student.FullName}", html),
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed weekly report to guardian {Email}", guardian.Email);
            }
        }
    }

    private static string BuildReportBody(string studentName, IReadOnlyList<StudentGradeDto> grades)
    {
        if (grades.Count == 0)
        {
            return $"<p>This is the weekly progress report for <strong>{studentName}</strong>.</p>" +
                   "<p>No quiz activity was recorded this period.</p>";
        }

        var rows = string.Join("", grades.Take(10).Select(g =>
            $"<li>{g.QuizTitle}: {g.Score}/{g.Total} ({g.Percentage}%) on {g.SubmittedAt:dd MMM yyyy}</li>"));

        return $"<p>Weekly progress report for <strong>{studentName}</strong>:</p><ul>{rows}</ul>";
    }
}
