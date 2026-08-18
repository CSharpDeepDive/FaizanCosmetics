using FaizanCosmetics.Application.Common;
using FaizanCosmetics.Application.DTOs;
using FaizanCosmetics.Application.Interfaces;

namespace FaizanCosmetics.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _auditService;

    public AuthService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher, ICurrentUserService currentUser, IAuditService auditService)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
        _auditService = auditService;
    }

    public async Task<LoginResultDto?> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        var user = await _unitOfWork.Users.GetByUsernameAsync(username.Trim(), cancellationToken);
        if (user is null || !user.IsActive || !_passwordHasher.Verify(password, user.PasswordHash))
        {
            return null;
        }

        user.LastLoginDate = DateTime.UtcNow;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _currentUser.SetCurrentUser(user.Id, user.Username, user.FullName, user.Role, user.MaxDiscountPercent, user.CanOverrideCreditLimit);

        await _auditService.LogAsync(user.Id, "Login", "User", user.Id, null, null, $"User '{user.Username}' logged in.", cancellationToken);

        return new LoginResultDto
        {
            UserId = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Role = user.Role,
            MustChangePassword = user.MustChangePassword
        };
    }

    public Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is int userId)
        {
            // Fire-and-forget style audit is intentionally awaited here for reliability at shutdown.
            return LogoutInternalAsync(userId, cancellationToken);
        }
        _currentUser.Clear();
        return Task.CompletedTask;
    }

    private async Task LogoutInternalAsync(int userId, CancellationToken cancellationToken)
    {
        await _auditService.LogAsync(userId, "Logout", "User", userId, null, null, "User logged out.", cancellationToken);
        _currentUser.Clear();
    }

    public async Task ChangePasswordAsync(int userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken)
            ?? throw new ValidationAppException("User not found.");

        if (!_passwordHasher.Verify(currentPassword, user.PasswordHash))
        {
            throw new InvalidCredentialsException();
        }

        var settings = await _unitOfWork.AppSettings.GetAsync(cancellationToken);
        if (newPassword.Length < settings.MinimumPasswordLength)
        {
            throw new ValidationAppException($"Password must be at least {settings.MinimumPasswordLength} characters.");
        }

        user.PasswordHash = _passwordHasher.Hash(newPassword);
        user.MustChangePassword = false;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(userId, "PasswordChanged", "User", userId, null, null, "User changed their password.", cancellationToken);
    }
}
