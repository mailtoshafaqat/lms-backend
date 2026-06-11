using Lms.Modules.Assessments.Domain;
using Lms.Modules.Assessments.Infrastructure;
using Lms.Shared.Courses;
using Lms.Shared.Email;
using Lms.Shared.Enrollments;
using Lms.Shared.Tenancy;
using Lms.Shared.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lms.Modules.Assessments.Application;

public sealed class QuizBatchNotifier
{
    private readonly AssessmentsDbContext _db;
    private readonly ICourseScopeReader _scope;
    private readonly IEnrollmentReader _enrollments;
    private readonly ISubjectAccessService _subjects;
    private readonly IInstituteUserReader _users;
    private readonly IEmailSender _email;
    private readonly IBrandedEmailRenderer _brandedEmail;
    private readonly ILogger<QuizBatchNotifier> _logger;

    public QuizBatchNotifier(
        AssessmentsDbContext db,
        ICourseScopeReader scope,
        IEnrollmentReader enrollments,
        ISubjectAccessService subjects,
        IInstituteUserReader users,
        IEmailSender email,
        IBrandedEmailRenderer brandedEmail,
        ILogger<QuizBatchNotifier> logger)
    {
        _db = db;
        _scope = scope;
        _enrollments = enrollments;
        _subjects = subjects;
        _users = users;
        _email = email;
        _brandedEmail = brandedEmail;
        _logger = logger;
    }

    public async Task TryNotifyQuizBatchCompleteAsync(Quiz quiz, CancellationToken ct = default)
    {
        if (!quiz.NotifyTeachersOnBatchComplete || quiz.BatchNotifySent) return;
        if (quiz.TopicId is null) return;

        var topicScope = await _scope.GetTopicScopeAsync(quiz.TopicId.Value, ct);
        if (topicScope is null) return;

        var enrolled = await _enrollments.GetActiveUserIdsForBundleAsync(topicScope.BundleId, ct);
        if (enrolled.Count == 0) return;

        var submittedCount = await _db.Attempts
            .Where(a => a.QuizId == quiz.Id && a.SubmittedAt != null)
            .Select(a => a.UserId)
            .Distinct()
            .CountAsync(ct);

        var threshold = Math.Clamp(quiz.BatchCompleteThresholdPercent, 1, 100);
        if (submittedCount * 100 < enrolled.Count * threshold) return;

        quiz.BatchNotifySent = true;
        await _db.SaveChangesAsync(ct);

        await NotifySubjectTeachersAsync(
            quiz.TenantId,
            topicScope.SubjectId,
            topicScope.SubjectTitle,
            quiz.Title,
            submittedCount,
            enrolled.Count,
            ct);
    }

    public async Task TryNotifyMockExamBatchCompleteAsync(MockExam exam, CancellationToken ct = default)
    {
        if (!exam.NotifyTeachersOnBatchComplete || exam.BatchNotifySent) return;

        var subjectScope = await _scope.GetSubjectScopeAsync(exam.SubjectId, ct);
        if (subjectScope is null) return;

        var enrolled = await _enrollments.GetActiveUserIdsForBundleAsync(subjectScope.BundleId, ct);
        if (enrolled.Count == 0) return;

        var submittedCount = await _db.MockExamAttempts
            .Where(a => a.MockExamId == exam.Id && a.SubmittedAt != null)
            .Select(a => a.UserId)
            .Distinct()
            .CountAsync(ct);

        var threshold = Math.Clamp(exam.BatchCompleteThresholdPercent, 1, 100);
        if (submittedCount * 100 < enrolled.Count * threshold) return;

        exam.BatchNotifySent = true;
        await _db.SaveChangesAsync(ct);

        await NotifySubjectTeachersAsync(
            exam.TenantId,
            exam.SubjectId,
            exam.SubjectTitle,
            exam.Title,
            submittedCount,
            enrolled.Count,
            ct);
    }

    private async Task NotifySubjectTeachersAsync(
        Guid tenantId,
        Guid subjectId,
        string subjectTitle,
        string assessmentTitle,
        int submittedCount,
        int enrolledCount,
        CancellationToken ct)
    {
        var teacherIds = await _subjects.GetTeacherIdsForSubjectAsync(subjectId, ct);
        if (teacherIds.Count == 0) return;

        var contacts = await _users.GetTeacherContactsAsync(teacherIds, ct);
        if (contacts.Count == 0) return;

        var body =
            $"<p><strong>{submittedCount}</strong> of <strong>{enrolledCount}</strong> enrolled students " +
            $"have submitted <strong>{assessmentTitle}</strong> ({subjectTitle}).</p>" +
            "<p>Review progress and analytics in your teacher dashboard.</p>";

        var html = await _brandedEmail.RenderAsync(
            tenantId, $"Batch complete: {assessmentTitle}", body, ct);

        foreach (var contact in contacts)
        {
            try
            {
                await _email.SendForTenantAsync(
                    tenantId,
                    new EmailMessage(contact.Email, contact.FullName,
                        $"Students completed: {assessmentTitle}", html),
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed batch-complete email to {Email}", contact.Email);
            }
        }
    }
}
