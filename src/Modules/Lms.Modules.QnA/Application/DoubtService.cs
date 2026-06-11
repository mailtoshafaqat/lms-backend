using Lms.Modules.QnA.Domain;
using Lms.Modules.QnA.Infrastructure;
using Lms.Shared.Auth;
using Lms.Shared.Common;
using Lms.Shared.Courses;
using Lms.Shared.Email;
using Lms.Shared.Enrollments;
using Lms.Shared.Tenancy;
using Lms.Shared.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Lms.Modules.QnA.Application;

public sealed class DoubtService : IDoubtService
{
    private readonly QnADbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICourseScopeReader _scope;
    private readonly IEnrollmentReader _enrollments;
    private readonly IEnrolledSubjectsReader _enrolledSubjects;
    private readonly ISubjectAccessService _subjects;
    private readonly IUserDirectory _users;
    private readonly IInstituteUserReader _instituteUsers;
    private readonly IEmailSender _email;
    private readonly IBrandedEmailRenderer _brandedEmail;
    private readonly IConfiguration _config;
    private readonly ILogger<DoubtService> _logger;

    public DoubtService(
        QnADbContext db,
        ITenantContext tenant,
        ICourseScopeReader scope,
        IEnrollmentReader enrollments,
        IEnrolledSubjectsReader enrolledSubjects,
        ISubjectAccessService subjects,
        IUserDirectory users,
        IInstituteUserReader instituteUsers,
        IEmailSender email,
        IBrandedEmailRenderer brandedEmail,
        IConfiguration config,
        ILogger<DoubtService> logger)
    {
        _db = db;
        _tenant = tenant;
        _scope = scope;
        _enrollments = enrollments;
        _enrolledSubjects = enrolledSubjects;
        _subjects = subjects;
        _users = users;
        _instituteUsers = instituteUsers;
        _email = email;
        _brandedEmail = brandedEmail;
        _config = config;
        _logger = logger;
    }

    public Task<IReadOnlyList<AssignedSubjectDto>> GetEnrolledSubjectsAsync(
        Guid userId, CancellationToken ct = default) =>
        _enrolledSubjects.GetEnrolledSubjectsAsync(userId, ct);

    public async Task<IReadOnlyList<DoubtThreadSummaryDto>> ListStudentThreadsAsync(
        Guid userId, CancellationToken ct = default)
    {
        var rows = await _db.DoubtThreads.AsNoTracking()
            .Where(t => t.StudentUserId == userId)
            .OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt)
            .ToListAsync(ct);

        return rows.Select(t => MapSummary(t, includeStudentName: false)).ToList();
    }

    public async Task<Result<DoubtThreadDetailDto>> CreateThreadAsync(
        Guid userId, string role, CreateDoubtRequest request, CancellationToken ct = default)
    {
        var question = request.Question?.Trim() ?? string.Empty;
        if (question.Length < 3)
            return Result<DoubtThreadDetailDto>.Failure("Question is too short.");

        if (request.SubjectId == Guid.Empty)
            return Result<DoubtThreadDetailDto>.Failure("Subject is required.");

        var subjectScope = await _scope.GetSubjectScopeAsync(request.SubjectId, ct);
        if (subjectScope is null)
            return Result<DoubtThreadDetailDto>.Failure("Subject not found.");

        var access = await EnsureStudentEnrollmentAsync(userId, role, subjectScope.BundleId, ct);
        if (!access.Succeeded)
            return Result<DoubtThreadDetailDto>.Failure(access.Error!);

        string? topicTitle = null;
        if (request.TopicId is not null)
        {
            var topicScope = await _scope.GetTopicScopeAsync(request.TopicId.Value, ct);
            if (topicScope is null)
                return Result<DoubtThreadDetailDto>.Failure("Topic not found.");
            if (topicScope.SubjectId != request.SubjectId)
                return Result<DoubtThreadDetailDto>.Failure("Topic does not belong to the selected subject.");
            topicTitle = topicScope.TopicTitle;
        }

        var bundleTitle = await ResolveBundleTitleAsync(userId, subjectScope, ct);
        var studentName = await ResolveNameAsync(userId, ct);

        var now = DateTime.UtcNow;
        var thread = new DoubtThread
        {
            TenantId = _tenant.TenantId,
            SubjectId = subjectScope.SubjectId,
            SubjectTitle = subjectScope.SubjectTitle,
            BundleId = subjectScope.BundleId,
            BundleTitle = bundleTitle,
            StudentUserId = userId,
            StudentName = studentName,
            TopicId = request.TopicId,
            TopicTitle = topicTitle,
            Title = TruncateTitle(question),
            Status = DoubtThreadStatus.Open,
            CreatedAt = now,
            UpdatedAt = now
        };

        var message = new DoubtMessage
        {
            TenantId = _tenant.TenantId,
            Thread = thread,
            AuthorUserId = userId,
            AuthorName = studentName,
            AuthorRole = Roles.Student,
            Body = question,
            CreatedAt = now
        };

        _db.DoubtThreads.Add(thread);
        _db.DoubtMessages.Add(message);
        await _db.SaveChangesAsync(ct);

        await TryNotifyTeachersAsync(thread, studentName, question, ct);

        return Result<DoubtThreadDetailDto>.Success(MapDetail(thread, [message]));
    }

    public async Task<Result<DoubtThreadDetailDto>> GetStudentThreadAsync(
        Guid userId, Guid threadId, CancellationToken ct = default)
    {
        var thread = await LoadThreadWithMessagesAsync(threadId, ct);
        if (thread is null) return Result<DoubtThreadDetailDto>.Failure(DoubtErrors.NotFound);
        if (thread.StudentUserId != userId)
            return Result<DoubtThreadDetailDto>.Failure(DoubtErrors.Forbidden);

        return Result<DoubtThreadDetailDto>.Success(MapDetail(thread, thread.Messages));
    }

    public async Task<Result<DoubtThreadDetailDto>> AddStudentMessageAsync(
        Guid userId, Guid threadId, string body, CancellationToken ct = default)
    {
        var text = body?.Trim() ?? string.Empty;
        if (text.Length < 1)
            return Result<DoubtThreadDetailDto>.Failure("Message body is required.");

        var thread = await _db.DoubtThreads
            .Include(t => t.Messages)
            .FirstOrDefaultAsync(t => t.Id == threadId, ct);

        if (thread is null) return Result<DoubtThreadDetailDto>.Failure(DoubtErrors.NotFound);
        if (thread.StudentUserId != userId)
            return Result<DoubtThreadDetailDto>.Failure(DoubtErrors.Forbidden);
        if (thread.Status != DoubtThreadStatus.Open)
            return Result<DoubtThreadDetailDto>.Failure("This thread is resolved and cannot receive new messages.");

        var authorName = await ResolveNameAsync(userId, ct);
        var now = DateTime.UtcNow;
        var message = new DoubtMessage
        {
            TenantId = _tenant.TenantId,
            ThreadId = thread.Id,
            AuthorUserId = userId,
            AuthorName = authorName,
            AuthorRole = Roles.Student,
            Body = text,
            CreatedAt = now
        };

        thread.UpdatedAt = now;
        _db.DoubtMessages.Add(message);
        await _db.SaveChangesAsync(ct);

        return Result<DoubtThreadDetailDto>.Success(MapDetail(thread, thread.Messages));
    }

    public async Task<PagedResult<DoubtThreadSummaryDto>> ListAdminThreadsAsync(
        Guid userId, string role, string? statusFilter, PagedListQuery query, CancellationToken ct = default)
    {
        var q = _db.DoubtThreads.AsNoTracking().AsQueryable();

        if (!_subjects.HasInstituteWideAccess(role))
        {
            var assigned = await _subjects.GetAssignedSubjectsAsync(userId, role, ct);
            var subjectIds = assigned.Select(s => s.SubjectId).ToList();
            if (subjectIds.Count == 0)
                return new PagedResult<DoubtThreadSummaryDto>([], query.NormalizedPage, query.NormalizedPageSize, 0);
            q = q.Where(t => subjectIds.Contains(t.SubjectId));
        }

        q = ApplyStatusFilter(q, statusFilter);

        if (query.NormalizedSearch is { } term)
        {
            var lower = term.ToLowerInvariant();
            q = q.Where(t =>
                t.Title.ToLower().Contains(lower)
                || (t.StudentName != null && t.StudentName.ToLower().Contains(lower)));
        }

        q = ApplyDoubtSort(q, query);

        var page = query.NormalizedPage;
        var pageSize = query.NormalizedPageSize;
        var total = await q.CountAsync(ct);
        var rows = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var data = rows.Select(t => MapSummary(t, includeStudentName: true)).ToList();
        return new PagedResult<DoubtThreadSummaryDto>(data, page, pageSize, total);
    }

    public async Task<Result<DoubtThreadDetailDto>> GetAdminThreadAsync(
        Guid userId, string role, Guid threadId, CancellationToken ct = default)
    {
        var thread = await LoadThreadWithMessagesAsync(threadId, ct);
        if (thread is null) return Result<DoubtThreadDetailDto>.Failure(DoubtErrors.NotFound);

        if (!await CanViewThreadAsync(userId, role, thread.SubjectId, ct))
            return Result<DoubtThreadDetailDto>.Failure(DoubtErrors.Forbidden);

        return Result<DoubtThreadDetailDto>.Success(MapDetail(thread, thread.Messages));
    }

    public async Task<Result<DoubtThreadDetailDto>> ReplyAsTeacherAsync(
        Guid userId, string role, Guid threadId, string body, CancellationToken ct = default)
    {
        var text = body?.Trim() ?? string.Empty;
        if (text.Length < 1)
            return Result<DoubtThreadDetailDto>.Failure("Message body is required.");

        var thread = await _db.DoubtThreads
            .Include(t => t.Messages)
            .FirstOrDefaultAsync(t => t.Id == threadId, ct);

        if (thread is null) return Result<DoubtThreadDetailDto>.Failure(DoubtErrors.NotFound);
        if (!await _subjects.CanManageSubjectAsync(userId, role, thread.SubjectId, ct))
            return Result<DoubtThreadDetailDto>.Failure(DoubtErrors.Forbidden);
        if (thread.Status != DoubtThreadStatus.Open)
            return Result<DoubtThreadDetailDto>.Failure("This thread is resolved.");

        var authorName = await ResolveNameAsync(userId, ct);
        var now = DateTime.UtcNow;
        var message = new DoubtMessage
        {
            TenantId = _tenant.TenantId,
            ThreadId = thread.Id,
            AuthorUserId = userId,
            AuthorName = authorName,
            AuthorRole = role,
            Body = text,
            CreatedAt = now
        };

        thread.UpdatedAt = now;
        _db.DoubtMessages.Add(message);
        await _db.SaveChangesAsync(ct);

        return Result<DoubtThreadDetailDto>.Success(MapDetail(thread, thread.Messages));
    }

    public async Task<Result<DoubtThreadDetailDto>> ResolveThreadAsync(
        Guid userId, string role, Guid threadId, CancellationToken ct = default)
    {
        var thread = await _db.DoubtThreads
            .Include(t => t.Messages)
            .FirstOrDefaultAsync(t => t.Id == threadId, ct);

        if (thread is null) return Result<DoubtThreadDetailDto>.Failure(DoubtErrors.NotFound);

        var canResolve = _subjects.HasInstituteWideAccess(role)
            || await _subjects.CanManageSubjectAsync(userId, role, thread.SubjectId, ct);

        if (!canResolve)
            return Result<DoubtThreadDetailDto>.Failure(DoubtErrors.Forbidden);

        if (thread.Status == DoubtThreadStatus.Resolved)
            return Result<DoubtThreadDetailDto>.Failure("Thread is already resolved.");

        var now = DateTime.UtcNow;
        thread.Status = DoubtThreadStatus.Resolved;
        thread.ResolvedAt = now;
        thread.ResolvedByUserId = userId;
        thread.UpdatedAt = now;
        await _db.SaveChangesAsync(ct);

        return Result<DoubtThreadDetailDto>.Success(MapDetail(thread, thread.Messages.OrderBy(m => m.CreatedAt).ToList()));
    }

    private async Task<DoubtThread?> LoadThreadWithMessagesAsync(Guid threadId, CancellationToken ct) =>
        await _db.DoubtThreads.AsNoTracking()
            .Include(t => t.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(t => t.Id == threadId, ct);

    private async Task<bool> CanViewThreadAsync(
        Guid userId, string role, Guid subjectId, CancellationToken ct)
    {
        if (_subjects.HasInstituteWideAccess(role)) return true;
        return await _subjects.CanManageSubjectAsync(userId, role, subjectId, ct);
    }

    private async Task<Result> EnsureStudentEnrollmentAsync(
        Guid userId, string role, Guid bundleId, CancellationToken ct)
    {
        if (role is Roles.SuperAdmin or Roles.InstituteAdmin or Roles.Teacher)
            return Result.Success();

        var bundles = await _enrollments.GetActiveBundleIdsAsync(userId, ct);
        if (!bundles.Contains(bundleId))
            return Result.Failure("You are not enrolled in this course.");

        return Result.Success();
    }

    private async Task<string> ResolveBundleTitleAsync(
        Guid userId, SubjectScope subjectScope, CancellationToken ct)
    {
        var enrolled = await _enrolledSubjects.GetEnrolledSubjectsAsync(userId, ct);
        var match = enrolled.FirstOrDefault(s => s.SubjectId == subjectScope.SubjectId)
            ?? enrolled.FirstOrDefault(s => s.BundleId == subjectScope.BundleId);
        return match?.BundleTitle ?? "Course";
    }

    private async Task<string> ResolveNameAsync(Guid userId, CancellationToken ct)
    {
        var names = await _users.GetDisplayNamesAsync([userId], ct);
        return names.TryGetValue(userId, out var name) ? name : "User";
    }

    private async Task TryNotifyTeachersAsync(
        DoubtThread thread, string studentName, string question, CancellationToken ct)
    {
        var teacherIds = await _subjects.GetTeacherIdsForSubjectAsync(thread.SubjectId, ct);
        if (teacherIds.Count == 0) return;

        var teachers = await _instituteUsers.GetTeacherContactsAsync(teacherIds, ct);
        if (teachers.Count == 0) return;

        var baseUrl = (_config["App:BaseUrl"] ?? "http://localhost:3000").TrimEnd('/');
        var threadUrl = $"{baseUrl}/admin/doubts/{thread.Id}";
        var topicLine = thread.TopicTitle is { Length: > 0 } topic
            ? $"<p><strong>Topic:</strong> {topic}</p>"
            : string.Empty;

        var body =
            $"<p>Hi,</p>" +
            $"<p><strong>{studentName}</strong> asked a new question in <strong>{thread.SubjectTitle}</strong>.</p>" +
            topicLine +
            $"<p style=\"margin:1em 0;padding:0.75em 1em;background:#f8fafc;border-left:3px solid #334155;\">" +
            $"{System.Net.WebUtility.HtmlEncode(question)}</p>" +
            $"<p><a href=\"{threadUrl}\">View and reply in the admin portal</a></p>";

        foreach (var teacher in teachers)
        {
            try
            {
                var html = await _brandedEmail.RenderAsync(
                    _tenant.TenantId, "New student doubt", body, ct);
                await _email.SendForTenantAsync(
                    _tenant.TenantId,
                    new EmailMessage(
                        teacher.Email,
                        teacher.FullName,
                        $"New doubt in {thread.SubjectTitle}",
                        html),
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send doubt notification to teacher {TeacherId}",
                    teacher.UserId);
            }
        }
    }

    private static IQueryable<DoubtThread> ApplyDoubtSort(IQueryable<DoubtThread> query, PagedListQuery paging)
    {
        var sortBy = paging.SortBy?.Trim().ToLowerInvariant() ?? "updatedat";
        var desc = PagedQueryExtensions.ResolveDescending(paging, defaultDescending: true);

        return sortBy switch
        {
            "title" => desc ? query.OrderByDescending(t => t.Title) : query.OrderBy(t => t.Title),
            "createdat" => desc ? query.OrderByDescending(t => t.CreatedAt) : query.OrderBy(t => t.CreatedAt),
            _ => desc
                ? query.OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt)
                : query.OrderBy(t => t.UpdatedAt ?? t.CreatedAt),
        };
    }

    private static IQueryable<DoubtThread> ApplyStatusFilter(IQueryable<DoubtThread> query, string? statusFilter)
    {
        if (string.IsNullOrWhiteSpace(statusFilter) || statusFilter.Equals("all", StringComparison.OrdinalIgnoreCase))
            return query;

        if (statusFilter.Equals("open", StringComparison.OrdinalIgnoreCase))
            return query.Where(t => t.Status == DoubtThreadStatus.Open);

        if (statusFilter.Equals("resolved", StringComparison.OrdinalIgnoreCase))
            return query.Where(t => t.Status == DoubtThreadStatus.Resolved);

        return query;
    }

    private static string TruncateTitle(string text) =>
        text.Length <= 120 ? text : text[..117] + "…";

    private static DoubtThreadSummaryDto MapSummary(DoubtThread t, bool includeStudentName) => new(
        t.Id,
        t.SubjectId,
        t.SubjectTitle,
        t.BundleTitle,
        t.TopicId,
        t.TopicTitle,
        t.Title,
        t.Status.ToString(),
        includeStudentName ? t.StudentName : null,
        t.CreatedAt,
        t.UpdatedAt);

    private static DoubtThreadDetailDto MapDetail(DoubtThread t, IEnumerable<DoubtMessage> messages) => new(
        t.Id,
        t.SubjectId,
        t.SubjectTitle,
        t.BundleTitle,
        t.TopicId,
        t.TopicTitle,
        t.Title,
        t.Status.ToString(),
        t.StudentName,
        t.CreatedAt,
        t.UpdatedAt,
        t.ResolvedAt,
        messages
            .DistinctBy(m => m.Id)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new DoubtMessageDto(
                m.Id,
                m.AuthorUserId,
                m.AuthorName,
                m.AuthorRole,
                m.Body,
                m.CreatedAt))
            .ToList());
}
