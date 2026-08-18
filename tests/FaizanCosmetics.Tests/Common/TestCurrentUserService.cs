using FaizanCosmetics.Application.Interfaces;
using FaizanCosmetics.Domain.Enums;

namespace FaizanCosmetics.Tests.Common;

public class TestCurrentUserService : ICurrentUserService
{
    public int? UserId { get; private set; } = 1;
    public string? Username { get; private set; } = "test.user";
    public string? FullName { get; private set; } = "Test User";
    public UserRole? Role { get; private set; } = UserRole.Admin;
    public decimal MaxDiscountPercent { get; private set; } = 100;
    public bool CanOverrideCreditLimit { get; private set; } = true;
    public bool IsAuthenticated => UserId.HasValue;

    public void SetCurrentUser(int userId, string username, string fullName, UserRole role, decimal maxDiscountPercent, bool canOverrideCreditLimit)
    {
        UserId = userId; Username = username; FullName = fullName; Role = role;
        MaxDiscountPercent = maxDiscountPercent; CanOverrideCreditLimit = canOverrideCreditLimit;
    }

    public void Clear() => UserId = null;
}
