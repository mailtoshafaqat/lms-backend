using System.Security.Cryptography;
using Lms.Modules.Identity.Domain;
using Lms.Modules.Identity.Infrastructure;
using Lms.Shared.Auth;
using Lms.Shared.Common;
using Lms.Shared.Email;
using Lms.Shared.Courses;
using Lms.Shared.Enrollments;
using Lms.Shared.Progress;
using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Lms.Modules.Identity.Application;

/// <summary>Admin-managed account provisioning: there is no public self-signup. An admin creates a
/// student, optionally enrolls them into a course, and the temporary credentials are emailed.</summary>
public sealed class AdminUserService : IAdminUserService
{
    private const string PasswordAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";

    private readonly IdentityDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly ITenantContext _tenant;
    private readonly IEnrollmentWriter _enrollments;
    private readonly IEmailSender _email;
    private readonly IBrandedEmailRenderer _brandedEmail;
    private readonly ITenantFeaturesProvider _tenantFeatures;
    private readonly IConfiguration _config;
    private readonly IStudentGradesReader _grades;
    private readonly ISubjectCatalogReader _catalog;
    private readonly ILogger<AdminUserService> _logger;

    public AdminUserService(
        IdentityDbContext db,
        IPasswordHasher hasher,
        ITenantContext tenant,
        IEnrollmentWriter enrollments,
        IEmailSender email,
        IBrandedEmailRenderer brandedEmail,
        ITenantFeaturesProvider tenantFeatures,
        IStudentGradesReader grades,
        ISubjectCatalogReader catalog,
        IConfiguration config,
        ILogger<AdminUserService> logger)
    {
        _db = db;
        _hasher = hasher;
        _tenant = tenant;
        _enrollments = enrollments;
        _email = email;
        _brandedEmail = brandedEmail;
        _tenantFeatures = tenantFeatures;
        _grades = grades;
        _catalog = catalog;
        _config = config;
        _logger = logger;
    }

    public async Task<Result<CreatedStudentDto>> CreateStudentAsync(
        CreateStudentRequest request, CancellationToken ct = default)
    {
        var flags = await _tenantFeatures.GetAsync(_tenant.TenantId, ct);
        if (flags is not null && !flags.AllowAdminCreateStudent)
            return Result<CreatedStudentDto>.Failure("Student provisioning is disabled for this institute.");

        var email = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return Result<CreatedStudentDto>.Failure("A valid email is required.");
        if (string.IsNullOrWhiteSpace(request.FullName))
            return Result<CreatedStudentDto>.Failure("Full name is required.");
        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            return Result<CreatedStudentDto>.Failure("An account with this email already exists.");

        var tempPassword = GenerateTempPassword();
        var user = new User
        {
            TenantId = _tenant.TenantId,
            Email = email,
            FullName = request.FullName.Trim(),
            Country = NormalizeCountry(request.Country),
            PasswordHash = _hasher.Hash(tempPassword),
            Role = Roles.Student,
            Provider = AuthProvider.Local,
            MustChangePassword = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        EnrollmentSummary? enrollment = null;
        if (request.BundleId is { } bundleId && bundleId != Guid.Empty)
        {
            enrollment = await _enrollments.ProvisionEnrollAsync(user.Id, bundleId, ct);
            if (enrollment is null)
                _logger.LogWarning("Student {UserId} created but enrollment into {BundleId} failed.", user.Id, bundleId);
        }

        var emailSent = await TrySendWelcomeAsync(user, tempPassword, enrollment, ct);

        return Result<CreatedStudentDto>.Success(new CreatedStudentDto(
            user.Id, user.FullName, user.Email, tempPassword, emailSent,
            enrollment?.BundleTitle, enrollment?.ExpiresAt));
    }

    public async Task<PagedResult<StudentListItemDto>> ListStudentsAsync(
        PagedListQuery query, CancellationToken ct = default)
    {
        var q = _db.Users.AsNoTracking().Where(u => u.Role == Roles.Student);

        if (query.SubjectDefinitionId is Guid definitionId)
        {
            var enrolledIds = await _catalog.GetEnrolledStudentIdsForDefinitionAsync(definitionId, ct);
            if (enrolledIds.Count == 0)
            {
                return new PagedResult<StudentListItemDto>(
                    [], query.NormalizedPage, query.NormalizedPageSize, 0);
            }

            q = q.Where(u => enrolledIds.Contains(u.Id));
        }

        if (query.NormalizedSearch is { } term)
        {
            var lower = term.ToLowerInvariant();
            q = q.Where(u =>
                u.FullName.ToLower().Contains(lower) || u.Email.ToLower().Contains(lower));
        }

        q = ApplyUserSort(q, query, defaultSortBy: "createdAt", defaultDescending: true);

        return await q
            .Select(u => new StudentListItemDto(
                u.Id, u.FullName, u.Email, u.IsActive, u.CreatedAt, u.ProfilePictureUrl))
            .ToPagedResultAsync(query, ct);
    }

    public async Task<StudentProfileDto?> GetStudentProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId && u.Role == Roles.Student, ct);
        return user is null ? null : MapStudentProfile(user);
    }

    public async Task<Result<StudentProfileDto>> UpdateStudentProfileAsync(
        Guid userId, UpdateStudentProfileRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            return Result<StudentProfileDto>.Failure("Full name is required.");

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.Role == Roles.Student, ct);
        if (user is null)
            return Result<StudentProfileDto>.Failure("Student not found.");

        user.FullName = request.FullName.Trim();
        user.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        user.Country = NormalizeCountry(request.Country);
        user.ProfilePictureUrl = string.IsNullOrWhiteSpace(request.ProfilePictureUrl)
            ? null
            : request.ProfilePictureUrl.Trim();
        user.ProfileNotes = string.IsNullOrWhiteSpace(request.ProfileNotes)
            ? null
            : request.ProfileNotes.Trim();

        await _db.SaveChangesAsync(ct);
        return Result<StudentProfileDto>.Success(MapStudentProfile(user));
    }

    public async Task<Result<CreatedTeacherDto>> CreateTeacherAsync(
        CreateTeacherRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return Result<CreatedTeacherDto>.Failure("A valid email is required.");
        if (string.IsNullOrWhiteSpace(request.FullName))
            return Result<CreatedTeacherDto>.Failure("Full name is required.");
        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            return Result<CreatedTeacherDto>.Failure("An account with this email already exists.");

        var tempPassword = GenerateTempPassword();
        var user = new User
        {
            TenantId = _tenant.TenantId,
            Email = email,
            FullName = request.FullName.Trim(),
            PasswordHash = _hasher.Hash(tempPassword),
            Role = Roles.Teacher,
            Provider = AuthProvider.Local,
            MustChangePassword = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        var emailSent = await TrySendTeacherWelcomeAsync(user, tempPassword, ct);

        return Result<CreatedTeacherDto>.Success(new CreatedTeacherDto(
            user.Id, user.FullName, user.Email, tempPassword, emailSent));
    }

    public async Task<Result<StudentListItemDto>> SetStudentStatusAsync(
        Guid userId, bool isActive, CancellationToken ct = default)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.Role == Roles.Student, ct);
        if (user is null)
            return Result<StudentListItemDto>.Failure("Student not found.");

        user.IsActive = isActive;
        await _db.SaveChangesAsync(ct);

        return Result<StudentListItemDto>.Success(MapStudentListItem(user));
    }

    public async Task<Result<ResetStudentPasswordDto>> ResetStudentPasswordAsync(
        Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.Role == Roles.Student, ct);
        if (user is null)
            return Result<ResetStudentPasswordDto>.Failure("Student not found.");

        var tempPassword = GenerateTempPassword();
        user.PasswordHash = _hasher.Hash(tempPassword);
        user.MustChangePassword = true;
        await _db.SaveChangesAsync(ct);

        var emailSent = await TrySendPasswordResetAsync(user, tempPassword, ct);

        return Result<ResetStudentPasswordDto>.Success(new ResetStudentPasswordDto(
            user.Id, user.FullName, user.Email, tempPassword, emailSent));
    }

    public async Task<PagedResult<TeacherListItemDto>> ListTeachersAsync(
        PagedListQuery query, CancellationToken ct = default)
    {
        var q = _db.Users.AsNoTracking().Where(u => u.Role == Roles.Teacher);

        if (query.NormalizedSearch is { } term)
        {
            var lower = term.ToLowerInvariant();
            q = q.Where(u =>
                u.FullName.ToLower().Contains(lower) || u.Email.ToLower().Contains(lower));
        }

        q = ApplyUserSort(q, query, defaultSortBy: "createdAt", defaultDescending: true);

        return await q
            .Select(u => new TeacherListItemDto(u.Id, u.FullName, u.Email, u.IsActive, u.CreatedAt))
            .ToPagedResultAsync(query, ct);
    }

    public async Task<Result<TeacherListItemDto>> SetTeacherStatusAsync(
        Guid userId, bool isActive, CancellationToken ct = default)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.Role == Roles.Teacher, ct);
        if (user is null)
            return Result<TeacherListItemDto>.Failure("Teacher not found.");

        user.IsActive = isActive;
        await _db.SaveChangesAsync(ct);

        return Result<TeacherListItemDto>.Success(new TeacherListItemDto(
            user.Id, user.FullName, user.Email, user.IsActive, user.CreatedAt));
    }

    public async Task<Result<ResetTeacherPasswordDto>> ResetTeacherPasswordAsync(
        Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.Role == Roles.Teacher, ct);
        if (user is null)
            return Result<ResetTeacherPasswordDto>.Failure("Teacher not found.");

        var tempPassword = GenerateTempPassword();
        user.PasswordHash = _hasher.Hash(tempPassword);
        user.MustChangePassword = true;
        await _db.SaveChangesAsync(ct);

        var emailSent = await TrySendTeacherPasswordResetAsync(user, tempPassword, ct);

        return Result<ResetTeacherPasswordDto>.Success(new ResetTeacherPasswordDto(
            user.Id, user.FullName, user.Email, tempPassword, emailSent));
    }

    private static IQueryable<User> ApplyUserSort(
        IQueryable<User> query, PagedListQuery paging, string defaultSortBy, bool defaultDescending)
    {
        var sortBy = paging.SortBy?.Trim().ToLowerInvariant() ?? defaultSortBy.ToLowerInvariant();
        var desc = PagedQueryExtensions.ResolveDescending(paging, defaultDescending);

        return sortBy switch
        {
            "fullname" => desc ? query.OrderByDescending(u => u.FullName) : query.OrderBy(u => u.FullName),
            "email" => desc ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
            _ => desc ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt),
        };
    }

    private async Task<bool> TrySendWelcomeAsync(
        User user, string tempPassword, EnrollmentSummary? enrollment, CancellationToken ct)
    {
        var loginUrl = (_config["App:BaseUrl"] ?? "http://localhost:3000").TrimEnd('/') + "/login";
        var courseLine = enrollment is null
            ? ""
            : $"<p>You have been enrolled in <strong>{enrollment.BundleTitle}</strong> " +
              $"(access until {enrollment.ExpiresAt:dd MMM yyyy}).</p>";

        var body =
            $"<p>Hi {user.FullName},</p>" +
            "<p>An account has been created for you on our learning platform.</p>" +
            courseLine +
            "<p><strong>Your login details:</strong></p>" +
            $"<ul><li>Email: {user.Email}</li><li>Temporary password: {tempPassword}</li></ul>" +
            $"<p>Please sign in at <a href=\"{loginUrl}\">{loginUrl}</a> and change your password.</p>";

        try
        {
            var html = await _brandedEmail.RenderAsync(user.TenantId, "Your account is ready", body, ct);
            await _email.SendForTenantAsync(
                user.TenantId,
                new EmailMessage(user.Email, user.FullName, "Your account is ready", html), ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to {Email}", user.Email);
            return false;
        }
    }

    private async Task<bool> TrySendTeacherWelcomeAsync(User user, string tempPassword, CancellationToken ct)
    {
        var loginUrl = (_config["App:BaseUrl"] ?? "http://localhost:3000").TrimEnd('/') + "/login";
        var body =
            $"<p>Hi {user.FullName},</p>" +
            "<p>You have been added as a <strong>teacher</strong> on our learning platform.</p>" +
            "<p>After signing in you can manage content and schedule live classes for your assigned subjects.</p>" +
            "<p><strong>Your login details:</strong></p>" +
            $"<ul><li>Email: {user.Email}</li><li>Temporary password: {tempPassword}</li></ul>" +
            $"<p>Please sign in at <a href=\"{loginUrl}\">{loginUrl}</a> and change your password.</p>";

        try
        {
            var html = await _brandedEmail.RenderAsync(user.TenantId, "Teacher account created", body, ct);
            await _email.SendForTenantAsync(
                user.TenantId,
                new EmailMessage(user.Email, user.FullName, "Teacher account created", html), ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send teacher welcome email to {Email}", user.Email);
            return false;
        }
    }

    private async Task<bool> TrySendTeacherPasswordResetAsync(User user, string tempPassword, CancellationToken ct)
    {
        var loginUrl = (_config["App:BaseUrl"] ?? "http://localhost:3000").TrimEnd('/') + "/login";
        var body =
            $"<p>Hi {user.FullName},</p>" +
            "<p>Your institute administrator reset your teacher account password.</p>" +
            "<p><strong>Your new temporary login details:</strong></p>" +
            $"<ul><li>Email: {user.Email}</li><li>Temporary password: {tempPassword}</li></ul>" +
            $"<p>Please sign in at <a href=\"{loginUrl}\">{loginUrl}</a> and choose a new password.</p>";

        try
        {
            var html = await _brandedEmail.RenderAsync(user.TenantId, "Your password was reset", body, ct);
            await _email.SendForTenantAsync(
                user.TenantId,
                new EmailMessage(user.Email, user.FullName, "Your password was reset", html), ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send teacher password reset email to {Email}", user.Email);
            return false;
        }
    }

    private async Task<bool> TrySendPasswordResetAsync(User user, string tempPassword, CancellationToken ct)
    {
        var loginUrl = (_config["App:BaseUrl"] ?? "http://localhost:3000").TrimEnd('/') + "/login";
        var body =
            $"<p>Hi {user.FullName},</p>" +
            "<p>Your institute administrator reset your account password.</p>" +
            "<p><strong>Your new temporary login details:</strong></p>" +
            $"<ul><li>Email: {user.Email}</li><li>Temporary password: {tempPassword}</li></ul>" +
            $"<p>Please sign in at <a href=\"{loginUrl}\">{loginUrl}</a> and choose a new password.</p>";

        try
        {
            var html = await _brandedEmail.RenderAsync(user.TenantId, "Your password was reset", body, ct);
            await _email.SendForTenantAsync(
                user.TenantId,
                new EmailMessage(user.Email, user.FullName, "Your password was reset", html), ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
            return false;
        }
    }

    public async Task<IReadOnlyList<StudentGuardianDto>> ListGuardiansAsync(
        Guid studentUserId, CancellationToken ct = default)
    {
        var rows = await _db.StudentGuardians.AsNoTracking()
            .Where(g => g.StudentUserId == studentUserId)
            .OrderBy(g => g.Name)
            .ToListAsync(ct);

        return rows.Select(MapGuardian).ToList();
    }

    public async Task<Result<StudentGuardianDto>> CreateGuardianAsync(
        Guid studentUserId, CreateStudentGuardianRequest request, CancellationToken ct = default)
    {
        if (!await _db.Users.AnyAsync(u => u.Id == studentUserId && u.Role == Roles.Student, ct))
            return Result<StudentGuardianDto>.Failure("Student not found.");

        var email = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result<StudentGuardianDto>.Failure("Guardian name is required.");
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return Result<StudentGuardianDto>.Failure("A valid guardian email is required.");

        var entity = new StudentGuardian
        {
            TenantId = _tenant.TenantId,
            StudentUserId = studentUserId,
            Name = request.Name.Trim(),
            Email = email,
            WeeklyReportsEnabled = request.WeeklyReportsEnabled
        };

        _db.StudentGuardians.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Result<StudentGuardianDto>.Success(MapGuardian(entity));
    }

    public async Task<Result<StudentGuardianDto>> UpdateGuardianAsync(
        Guid guardianId, UpdateStudentGuardianRequest request, CancellationToken ct = default)
    {
        var entity = await _db.StudentGuardians.FirstOrDefaultAsync(g => g.Id == guardianId, ct);
        if (entity is null) return Result<StudentGuardianDto>.Failure("Guardian not found.");

        var email = request.Email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result<StudentGuardianDto>.Failure("Guardian name is required.");
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return Result<StudentGuardianDto>.Failure("A valid guardian email is required.");

        entity.Name = request.Name.Trim();
        entity.Email = email;
        entity.WeeklyReportsEnabled = request.WeeklyReportsEnabled;
        await _db.SaveChangesAsync(ct);
        return Result<StudentGuardianDto>.Success(MapGuardian(entity));
    }

    public async Task<bool> DeleteGuardianAsync(Guid guardianId, CancellationToken ct = default)
    {
        var entity = await _db.StudentGuardians.FirstOrDefaultAsync(g => g.Id == guardianId, ct);
        if (entity is null) return false;
        _db.StudentGuardians.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<Result<SendGuardianReportResultDto>> SendGuardianReportAsync(
        Guid studentUserId, Guid guardianId, CancellationToken ct = default)
    {
        var student = await _db.Users.FirstOrDefaultAsync(
            u => u.Id == studentUserId && u.Role == Roles.Student, ct);
        if (student is null) return Result<SendGuardianReportResultDto>.Failure("Student not found.");

        var guardian = await _db.StudentGuardians.FirstOrDefaultAsync(
            g => g.Id == guardianId && g.StudentUserId == studentUserId, ct);
        if (guardian is null) return Result<SendGuardianReportResultDto>.Failure("Guardian not found.");

        var grades = await _grades.GetRecentGradesAsync(studentUserId, ct: ct);
        var emailSent = await TrySendGuardianReportAsync(student, guardian, grades, ct);
        return Result<SendGuardianReportResultDto>.Success(new SendGuardianReportResultDto(emailSent));
    }

    private async Task<bool> TrySendGuardianReportAsync(
        User student, StudentGuardian guardian, IReadOnlyList<StudentGradeDto> grades, CancellationToken ct)
    {
        var rows = grades.Count == 0
            ? "<p>No quiz activity recorded yet.</p>"
            : "<ul>" + string.Join("", grades.Take(15).Select(g =>
                $"<li><strong>{g.QuizTitle}</strong>: {g.Score}/{g.Total} ({g.Percentage}%) " +
                $"on {g.SubmittedAt:dd MMM yyyy}</li>")) + "</ul>";

        var body =
            $"<p>Hi {guardian.Name},</p>" +
            $"<p>Here is the latest progress report for <strong>{student.FullName}</strong>:</p>" +
            rows;

        try
        {
            var html = await _brandedEmail.RenderAsync(
                student.TenantId, $"Progress report: {student.FullName}", body, ct);
            await _email.SendForTenantAsync(
                student.TenantId,
                new EmailMessage(guardian.Email, guardian.Name,
                    $"Progress report: {student.FullName}", html),
                ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send guardian report to {Email}", guardian.Email);
            return false;
        }
    }

    private static StudentGuardianDto MapGuardian(StudentGuardian g) =>
        new(g.Id, g.StudentUserId, g.Name, g.Email, g.WeeklyReportsEnabled);

    private static StudentListItemDto MapStudentListItem(User user) =>
        new(user.Id, user.FullName, user.Email, user.IsActive, user.CreatedAt, user.ProfilePictureUrl);

    private static StudentProfileDto MapStudentProfile(User user) =>
        new(
            user.Id,
            user.FullName,
            user.Email,
            user.Phone,
            user.Country,
            user.ProfilePictureUrl,
            user.ProfileNotes,
            user.IsActive,
            user.CreatedAt);

    private static string? NormalizeCountry(string? code) =>
        string.IsNullOrWhiteSpace(code)
            ? null
            : code.Trim().ToUpperInvariant()[..Math.Min(2, code.Trim().Length)];

    private static string GenerateTempPassword(int length = 12)
    {
        var chars = new char[length];
        for (var i = 0; i < length; i++)
            chars[i] = PasswordAlphabet[RandomNumberGenerator.GetInt32(PasswordAlphabet.Length)];
        return new string(chars);
    }
}
