using System.Security.Cryptography;
using Lms.Modules.Identity.Domain;
using Lms.Modules.Identity.Infrastructure;
using Lms.Shared.Auth;
using Lms.Shared.Common;
using Lms.Shared.Email;
using Lms.Shared.Enrollments;
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
    private readonly ILogger<AdminUserService> _logger;

    public AdminUserService(
        IdentityDbContext db,
        IPasswordHasher hasher,
        ITenantContext tenant,
        IEnrollmentWriter enrollments,
        IEmailSender email,
        IBrandedEmailRenderer brandedEmail,
        ITenantFeaturesProvider tenantFeatures,
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
            enrollment = await _enrollments.EnrollAsync(user.Id, bundleId, ct);
            if (enrollment is null)
                _logger.LogWarning("Student {UserId} created but enrollment into {BundleId} failed.", user.Id, bundleId);
        }

        var emailSent = await TrySendWelcomeAsync(user, tempPassword, enrollment, ct);

        return Result<CreatedStudentDto>.Success(new CreatedStudentDto(
            user.Id, user.FullName, user.Email, tempPassword, emailSent,
            enrollment?.BundleTitle, enrollment?.ExpiresAt));
    }

    public async Task<IReadOnlyList<StudentListItemDto>> ListStudentsAsync(CancellationToken ct = default)
    {
        return await _db.Users
            .Where(u => u.Role == Roles.Student)
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new StudentListItemDto(u.Id, u.FullName, u.Email, u.IsActive, u.CreatedAt))
            .ToListAsync(ct);
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

    private static string GenerateTempPassword(int length = 12)
    {
        var chars = new char[length];
        for (var i = 0; i < length; i++)
            chars[i] = PasswordAlphabet[RandomNumberGenerator.GetInt32(PasswordAlphabet.Length)];
        return new string(chars);
    }
}
