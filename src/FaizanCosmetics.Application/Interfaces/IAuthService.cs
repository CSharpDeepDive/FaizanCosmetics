using FaizanCosmetics.Application.DTOs;

namespace FaizanCosmetics.Application.Interfaces;

public interface IAuthService
{
    /// <summary>Validates credentials, records login (LastLoginDate + AuditLog), and populates ICurrentUserService. Returns null on failure.</summary>
    Task<LoginResultDto?> LoginAsync(string username, string password, CancellationToken cancellationToken = default);

    Task LogoutAsync(CancellationToken cancellationToken = default);

    Task ChangePasswordAsync(int userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
}
