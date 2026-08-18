using FaizanCosmetics.Domain.Common;
using FaizanCosmetics.Domain.Enums;

namespace FaizanCosmetics.Domain.Entities;

/// <summary>A manual stock correction. Always produces exactly one InventoryTransaction (AdjustmentIncrease/AdjustmentDecrease/Damage/Theft/Expiry/OpeningStock) and is fully audited.</summary>
public class InventoryAdjustment : BaseEntity
{
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    /// <summary>Positive quantity; IsIncrease determines direction.</summary>
    public decimal Quantity { get; set; }
    public bool IsIncrease { get; set; }
    public AdjustmentReason Reason { get; set; }
    public string? Notes { get; set; }

    public DateTime AdjustmentDate { get; set; } = DateTime.UtcNow;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int? InventoryTransactionId { get; set; }
    public InventoryTransaction? InventoryTransaction { get; set; }
}
