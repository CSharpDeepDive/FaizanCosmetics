using FaizanCosmetics.Domain.Common;
using FaizanCosmetics.Domain.Enums;

namespace FaizanCosmetics.Domain.Entities;

/// <summary>
/// The single source of truth for every stock movement. Product.CurrentStock is a cache
/// derived from these rows — every stock change must create one of these, inside the same
/// database transaction as whatever caused it (sale, purchase, return, adjustment, etc.).
/// </summary>
public class InventoryTransaction : BaseEntity
{
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public InventoryTransactionType TransactionType { get; set; }

    /// <summary>Always positive; direction is implied by TransactionType.</summary>
    public decimal Quantity { get; set; }
    public decimal PreviousStock { get; set; }
    public decimal NewStock { get; set; }

    /// <summary>Unit cost at the time of this movement (used for historical COGS/profit calculations).</summary>
    public decimal UnitCost { get; set; }

    public ReferenceType ReferenceType { get; set; }
    public int ReferenceId { get; set; }

    public int? BatchId { get; set; }
    public ProductBatch? Batch { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
}
