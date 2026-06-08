using System.Security.Cryptography;
using Lms.Modules.Identity.Domain;
using Lms.Modules.Identity.Infrastructure;
using Lms.Shared.Auth;
using Lms.Shared.Common;
using Lms.Shared.Tenancy;
using Lms.Shared.Users;
using Microsoft.EntityFrameworkCore;

namespace Lms.Modules.Identity.Application;

/// <summary>SuperAdmin provisions InstituteAdmin accounts on a specific tenant (cross-tenant write).</summary>
public sealed class InstituteAdminProvisioner : IInstituteAdminProvisioner
{
    private const string PasswordAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";

    private readonly IdentityDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly ITenantFeaturesProvider _tenants;

    public InstituteAdminProvisioner(
        IdentityDbContext db, IPasswordHasher hasher, ITenantFeaturesProvider tenants)
    {
        _db = db;
        _hasher = hasher;
        _tenants = tenants;
    }

    public async Task<Result<CreatedInstituteAdminDto>> CreateAsync(
        Guid tenantId, string email, string fullName, CancellationToken ct = default)
    {
        if (tenantId == TenantContext.SystemTenantId)
            return Result<CreatedInstituteAdminDto>.Failure("Cannot provision an institute admin on the system tenant.");

        var tenant = await _tenants.GetAsync(tenantId, ct);
        if (tenant is null)
            return Result<CreatedInstituteAdminDto>.Failure("Tenant not found.");

        var normalized = email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized) || !normalized.Contains('@'))
            return Result<CreatedInstituteAdminDto>.Failure("A valid email is required.");
        if (string.IsNullOrWhiteSpace(fullName))
            return Result<CreatedInstituteAdminDto>.Failure("Full name is required.");

        var exists = await _db.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.TenantId == tenantId && u.Email == normalized, ct);
        if (exists)
            return Result<CreatedInstituteAdminDto>.Failure("An account with this email already exists on this tenant.");

        var tempPassword = GenerateTempPassword();
        var user = new User
        {
            TenantId = tenantId,
            Email = normalized,
            FullName = fullName.Trim(),
            PasswordHash = _hasher.Hash(tempPassword),
            Role = Roles.InstituteAdmin,
            Provider = AuthProvider.Local,
            MustChangePassword = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return Result<CreatedInstituteAdminDto>.Success(new CreatedInstituteAdminDto(
            user.Id, user.FullName, user.Email, tempPassword, tenantId));
    }

    private static string GenerateTempPassword(int length = 12)
    {
        var chars = new char[length];
        for (var i = 0; i < length; i++)
            chars[i] = PasswordAlphabet[RandomNumberGenerator.GetInt32(PasswordAlphabet.Length)];
        return new string(chars);
    }
}
