using FaizanCosmetics.Domain.Common;
using FaizanCosmetics.Domain.Enums;

namespace FaizanCosmetics.Domain.Entities;

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginDate { get; set; }

    /// <summary>Forces a password change on next login. Set true for seeded/reset accounts.</summary>
    public bool MustChangePassword { get; set; }

    /// <summary>Maximum discount percentage this user may apply without an override from a higher role.</summary>
    public decimal MaxDiscountPercent { get; set; }

    public bool CanOverrideCreditLimit { get; set; }

    public ICollection<SalesInvoice> SalesInvoices { get; set; } = new List<SalesInvoice>();
}
