using FaizanCosmetics.Domain.Common;

namespace FaizanCosmetics.Domain.Entities;

public class Product : SoftDeletableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    /// <summary>Current/latest cost price. Historical sales use the snapshot/cost captured at sale time, not this value.</summary>
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal WholesalePrice { get; set; }

    /// <summary>
    /// Denormalized running total for fast reads. This is a projection maintained exclusively by
    /// InventoryTransaction postings (see IInventoryService) — never write to it directly from
    /// application code outside that service.
    /// </summary>
    public decimal CurrentStock { get; set; }

    public decimal MinimumStockLevel { get; set; }
    public decimal ReorderLevel { get; set; }
    public string? Description { get; set; }
    public string? ImagePath { get; set; }
    public bool HasExpiry { get; set; }

    public ICollection<ProductPriceHistory> PriceHistory { get; set; } = new List<ProductPriceHistory>();
    public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
    public ICollection<ProductBatch> Batches { get; set; } = new List<ProductBatch>();
}
