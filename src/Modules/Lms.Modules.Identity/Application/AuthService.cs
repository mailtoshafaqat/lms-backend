using System.Security.Cryptography;
using Lms.Modules.Identity.Contracts;
using Lms.Modules.Identity.Domain;
using Lms.Modules.Identity.Infrastructure;
using Lms.Shared.Auth;
using Lms.Shared.Common;
using Lms.Shared.Email;
using Lms.Shared.Events;
using Lms.Shared.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lms.Modules.Identity.Application;

public sealed class AuthService : IAuthService
{
    private readonly IdentityDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _tokens;
    private readonly ITenantContext _tenant;
    private readonly ITenantFeaturesProvider _tenantFeatures;
    private readonly IEmailSender _email;
    private readonly IBrandedEmailRenderer _brandedEmail;
    private readonly IConfiguration _config;
    private readonly IEventBus _events;
    private readonly ILogger<AuthService> _logger;
    private readonly JwtOptions _jwt;

    public AuthService(
        IdentityDbContext db,
        IPasswordHasher hasher,
        IJwtTokenService tokens,
        ITenantContext tenant,
        ITenantFeaturesProvider tenantFeatures,
        IEmailSender email,
        IBrandedEmailRenderer brandedEmail,
        IConfiguration config,
        IEventBus events,
        ILogger<AuthService> logger,
        IOptions<JwtOptions> jwt)
    {
        _db = db;
        _hasher = hasher;
        _tokens = tokens;
        _tenant = tenant;
        _tenantFeatures = tenantFeatures;
        _email = email;
        _brandedEmail = brandedEmail;
        _config = config;
        _events = events;
        _logger = logger;
        _jwt = jwt.Value;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            return Result<AuthResponse>.Failure("An account with this email already exists.");

        var user = new User
        {
            TenantId = _tenant.TenantId,
            Email = email,
            FullName = request.FullName.Trim(),
            PasswordHash = _hasher.Hash(request.Password),
            Role = Roles.Student,
            Provider = AuthProvider.Local
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        await _events.PublishAsync(
            new UserRegisteredEvent(user.Id, user.TenantId, user.Email, user.Role), ct);

        return await IssueAsync(user, ct);
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == email, ct);

        if (user is null || !user.IsActive || user.PasswordHash is null ||
            !_hasher.Verify(request.Password, user.PasswordHash))
        {
            return Result<AuthResponse>.Failure("Invalid email or password.");
        }

        return await IssueAsync(user, ct);
    }

    public async Task<Result<AuthResponse>> RefreshAsync(RefreshRequest request, CancellationToken ct = default)
    {
        var token = await _db.RefreshTokens
            .IgnoreQueryFilters()
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == request.RefreshToken, ct);

        if (token is null || !token.IsActive || token.User is null)
            return Result<AuthResponse>.Failure("Invalid or expired refresh token.");

        token.RevokedAt = DateTime.UtcNow;
        return await IssueAsync(token.User, ct);
    }

    private async Task<Result<AuthResponse>?> ValidateTenantAccessAsync(User user, CancellationToken ct)
    {
        var features = await _tenantFeatures.GetAsync(user.TenantId, ct);
        if (features is null)
            return Result<AuthResponse>.Failure("Institute account not found.");
        if (features.Status == TenantStatus.Suspended)
            return Result<AuthResponse>.Failure("This institute account is suspended. Contact support.");
        if (TenantTrial.IsExpired(features.Status, features.TrialEndsAt)
            && user.Role is Roles.Teacher or Roles.Student)
        {
            return Result<AuthResponse>.Failure(
                "This institute is temporarily unavailable. Please contact your academy.");
        }

        return null;
    }

    private async Task<Result<AuthResponse>> IssueAsync(User user, CancellationToken ct)
    {
        if (!Roles.IsPlatformStaff(user.Role))
        {
            var accessError = await ValidateTenantAccessAsync(user, ct);
            if (accessError is not null) return accessError;
        }

        var (accessToken, expiresAt) = _tokens.CreateAccessToken(user);

        var refresh = new RefreshToken
        {
            UserId = user.Id,
            Token = _tokens.CreateRefreshToken(),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwt.RefreshTokenDays)
        };
        _db.RefreshTokens.Add(refresh);
        await _db.SaveChangesAsync(ct);

        var tenantDto = await MapTenantAsync(user, ct);

        return Result<AuthResponse>.Success(new AuthResponse(
            user.Id, user.Email, user.FullName, user.Role,
            accessToken, refresh.Token, expiresAt, user.MustChangePassword, tenantDto));
    }

    private async Task<TenantFeaturesDto?> MapTenantAsync(User user, CancellationToken ct)
    {
        if (Roles.IsPlatformStaff(user.Role)) return null;

        var f = await _tenantFeatures.GetAsync(user.TenantId, ct);
        if (f is null) return null;

        DateTime? trialEndsAt = null;
        int? trialDaysRemaining = null;
        bool? trialExpired = null;
        if (user.Role == Roles.InstituteAdmin && f.Status == TenantStatus.Trial)
        {
            trialEndsAt = f.TrialEndsAt;
            trialDaysRemaining = TenantTrial.DaysRemaining(f.TrialEndsAt);
            trialExpired = TenantTrial.IsExpired(f.Status, f.TrialEndsAt);
        }

        return new TenantFeaturesDto(
            f.TenantId, f.TenantName, f.Slug, f.Status.ToString(), f.Plan,
            f.ProductProfile.ToString(),
            f.MockExamsEnabled, f.UnitPyqTestsEnabled, f.MistakeDiaryEnabled, f.DoubtsEnabled,
            f.SyllabusMentorEnabled,
            f.LiveClassesEnabled, f.ZoomMode.ToString(), f.PaymentMode.ToString(),
            f.AllowStudentSelfEnroll, f.AllowAdminCreateStudent,
            f.BundlePriceEditEnabled, f.McqBulkImportEnabled,
            trialEndsAt, trialDaysRemaining, trialExpired);
    }

    public async Task<Result<bool>> ChangePasswordAsync(
        Guid userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var currentPassword = request.CurrentPassword.Trim();
        var newPassword = request.NewPassword.Trim();

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
            return Result<bool>.Failure("New password must be at least 8 characters.");

        // Ignore tenant filter — JWT user id is authoritative; filter can mismatch on auth endpoints.
        var user = await _db.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null || user.PasswordHash is null)
            return Result<bool>.Failure("Account not found.");

        if (!_hasher.Verify(currentPassword, user.PasswordHash))
            return Result<bool>.Failure(
                user.MustChangePassword
                    ? "Temporary password is incorrect. Use the exact password shown when your account was created."
                    : "Current password is incorrect.");

        user.PasswordHash = _hasher.Hash(newPassword);
        user.MustChangePassword = false;
        await _db.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive, ct);

        // Always succeed to avoid revealing whether the email exists.
        if (user is null || user.PasswordHash is null) return Result<bool>.Success(true);

        var existing = await _db.PasswordResetTokens
            .Where(t => t.UserId == user.Id && t.UsedAt == null)
            .ToListAsync(ct);
        foreach (var t in existing) t.UsedAt = DateTime.UtcNow;

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

        _db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        });
        await _db.SaveChangesAsync(ct);

        var features = await _tenantFeatures.GetAsync(user.TenantId, ct);
        var slug = features?.Slug ?? "demo";
        var instituteName = features?.TenantName ?? "Your institute";
        var baseUrl = (_config["App:BaseUrl"] ?? "http://localhost:3000").TrimEnd('/');
        var link = $"{baseUrl}/reset-password?token={Uri.EscapeDataString(token)}&tenant={Uri.EscapeDataString(slug)}";

        var body =
            $"<p>Hi {user.FullName},</p>" +
            $"<p>We received a request to reset your password for <strong>{instituteName}</strong>.</p>" +
            $"<p><a href=\"{link}\">Reset your password</a> (link expires in 1 hour).</p>" +
            "<p>If you did not request this, you can ignore this email.</p>";

        try
        {
            var html = await _brandedEmail.RenderAsync(user.TenantId, "Reset your password", body, ct);
            await _email.SendForTenantAsync(
                user.TenantId,
                new EmailMessage(user.Email, user.FullName, "Reset your password", html),
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
        }

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
            return Result<bool>.Failure("Password must be at least 8 characters.");

        var row = await _db.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.Token == request.Token, ct);

        if (row is null || !row.IsValid)
            return Result<bool>.Failure("This reset link is invalid or has expired.");

        var user = await _db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == row.UserId, ct);
        if (user is null || !user.IsActive)
            return Result<bool>.Failure("This reset link is invalid or has expired.");

        user.PasswordHash = _hasher.Hash(request.NewPassword);
        user.MustChangePassword = false;
        row.UsedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }

    public async Task<UserProfile?> GetProfileAsync(Guid userId, CancellationToken ct = default)
    {
        if (userId == Guid.Empty) return null;

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        return user is null
            ? null
            : new UserProfile(
                user.Id, user.Email, user.FullName, user.Role, user.Phone, user.ProfilePictureUrl);
    }
}
