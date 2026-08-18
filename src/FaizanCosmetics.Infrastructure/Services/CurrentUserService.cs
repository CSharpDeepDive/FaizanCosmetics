using FaizanCosmetics.Application.Interfaces;
using FaizanCosmetics.Domain.Enums;

namespace FaizanCosmetics.Infrastructure.Services;

/// <summary>Registered as a singleton: one desktop process = one logged-in operator at a time.</summary>
public class CurrentUserService : ICurrentUserService
{
    public int? UserId { get; private set; }
    public string? Username { get; private set; }
    public string? FullName { get; private set; }
    public UserRole? Role { get; private set; }
    public decimal MaxDiscountPercent { get; private set; }
    public bool CanOverrideCreditLimit { get; private set; }
    public bool IsAuthenticated => UserId.HasValue;

    public void SetCurrentUser(int userId, string username, string fullName, UserRole role, decimal maxDiscountPercent, bool canOverrideCreditLimit)
    {
        UserId = userId;
        Username = username;
        FullName = fullName;
        Role = role;
        MaxDiscountPercent = maxDiscountPercent;
        CanOverrideCreditLimit = canOverrideCreditLimit;
    }

    public void Clear()
    {
        UserId = null;
        Username = null;
        FullName = null;
        Role = null;
        MaxDiscountPercent = 0;
        CanOverrideCreditLimit = false;
    }
}
