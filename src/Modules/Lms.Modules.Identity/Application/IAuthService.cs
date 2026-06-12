using Lms.Shared.Common;

namespace Lms.Modules.Identity.Application;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<Result<AuthResponse>> RefreshAsync(RefreshRequest request, CancellationToken ct = default);
    Task<Result<bool>> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default);
    Task<Result<bool>> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default);
    Task<Result<bool>> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);
    Task<UserProfile?> GetProfileAsync(Guid userId, CancellationToken ct = default);
}
