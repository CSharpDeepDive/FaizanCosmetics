using FaizanCosmetics.Domain.Enums;

namespace FaizanCosmetics.Application.Interfaces;

/// <summary>Holds the identity of the operator currently logged into this desktop session. Registered as a singleton and populated by AuthService on successful login.</summary>
public interface ICurrentUserService
{
    int? UserId { get; }
    string? Username { get; }
    string? FullName { get; }
    UserRole? Role { get; }
    decimal MaxDiscountPercent { get; }
    bool CanOverrideCreditLimit { get; }
    bool IsAuthenticated { get; }

    void SetCurrentUser(int userId, string username, string fullName, UserRole role, decimal maxDiscountPercent, bool canOverrideCreditLimit);
    void Clear();
}
