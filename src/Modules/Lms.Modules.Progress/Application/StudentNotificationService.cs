using Lms.Modules.Progress.Domain;
using Lms.Modules.Progress.Infrastructure;
using Lms.Shared.Email;
using Lms.Shared.Notifications;
using Lms.Shared.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lms.Modules.Progress.Application;

public sealed class StudentNotificationService : IStudentNotificationService
{
    private readonly ProgressDbContext _db;
    private readonly IInstituteUserReader _users;
    private readonly IEmailSender _email;
    private readonly IBrandedEmailRenderer _brandedEmail;
    private readonly ILogger<StudentNotificationService> _logger;

    public StudentNotificationService(
        ProgressDbContext db,
        IInstituteUserReader users,
        IEmailSender email,
        IBrandedEmailRenderer brandedEmail,
        ILogger<StudentNotificationService> logger)
    {
        _db = db;
        _users = users;
        _email = email;
        _brandedEmail = brandedEmail;
        _logger = logger;
    }

    public Task NotifyAsync(CreateStudentNotificationRequest request, CancellationToken ct = default) =>
        NotifyManyAsync([request], ct);

    public async Task NotifyManyAsync(
        IReadOnlyList<CreateStudentNotificationRequest> requests,
        CancellationToken ct = default)
    {
        if (requests.Count == 0) return;

        var now = DateTime.UtcNow;
        foreach (var req in requests)
        {
            _db.UserNotifications.Add(new UserNotification
            {
                TenantId = req.TenantId,
                UserId = req.UserId,
                Title = req.Title.Trim(),
                Body = req.Body.Trim(),
                LinkUrl = string.IsNullOrWhiteSpace(req.LinkUrl) ? null : req.LinkUrl.Trim(),
                CreatedAt = now
            });
        }

        await _db.SaveChangesAsync(ct);

        var emailRequests = requests.Where(r => r.SendEmail).ToList();
        if (emailRequests.Count == 0) return;

        var contacts = await _users.GetStudentContactsAsync(emailRequests.Select(r => r.UserId), ct);
        var contactById = contacts.ToDictionary(c => c.UserId);

        foreach (var req in emailRequests)
        {
            if (!contactById.TryGetValue(req.UserId, out var contact)) continue;

            var subject = string.IsNullOrWhiteSpace(req.EmailSubject) ? req.Title : req.EmailSubject;
            var bodyHtml = $"<p>{req.Body}</p>";
            if (!string.IsNullOrWhiteSpace(req.LinkUrl))
                bodyHtml += $"<p><a href=\"{req.LinkUrl}\">Open in LMS</a></p>";

            try
            {
                var html = await _brandedEmail.RenderAsync(req.TenantId, subject, bodyHtml, ct);
                await _email.SendForTenantAsync(
                    req.TenantId,
                    new EmailMessage(contact.Email, contact.FullName, subject, html),
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed notification email to student {UserId}", req.UserId);
            }
        }
    }
}

public interface IStudentNotificationQueryService
{
    Task<IReadOnlyList<NotificationDto>> ListAsync(Guid userId, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);
    Task<bool> MarkReadAsync(Guid userId, Guid notificationId, CancellationToken ct = default);
    Task<int> MarkAllReadAsync(Guid userId, CancellationToken ct = default);
}

public sealed record NotificationDto(
    Guid Id,
    string Title,
    string Body,
    string? LinkUrl,
    bool IsRead,
    DateTime CreatedAt);

public sealed class StudentNotificationQueryService : IStudentNotificationQueryService
{
    private readonly ProgressDbContext _db;

    public StudentNotificationQueryService(ProgressDbContext db) => _db = db;

    public async Task<IReadOnlyList<NotificationDto>> ListAsync(Guid userId, CancellationToken ct = default) =>
        await _db.UserNotifications.AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new NotificationDto(
                n.Id, n.Title, n.Body, n.LinkUrl, n.IsRead, n.CreatedAt))
            .ToListAsync(ct);

    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default) =>
        _db.UserNotifications.AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.IsRead, ct);

    public async Task<bool> MarkReadAsync(Guid userId, Guid notificationId, CancellationToken ct = default)
    {
        var row = await _db.UserNotifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, ct);
        if (row is null) return false;
        if (row.IsRead) return true;

        row.IsRead = true;
        row.ReadAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<int> MarkAllReadAsync(Guid userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var unread = await _db.UserNotifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(ct);

        foreach (var row in unread)
        {
            row.IsRead = true;
            row.ReadAt = now;
        }

        if (unread.Count > 0) await _db.SaveChangesAsync(ct);
        return unread.Count;
    }
}
