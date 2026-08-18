using FaizanCosmetics.Domain.Common;

namespace FaizanCosmetics.Domain.Entities;

/// <summary>
/// Optional batch/expiry tracking for a Product. Only populated for products where
/// Product.HasExpiry is true; other products are never forced to carry a batch.
/// </summary>
public class ProductBatch : BaseEntity
{
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public string BatchNumber { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal Quantity { get; set; }
    public decimal RemainingQuantity { get; set; }

    public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
}
