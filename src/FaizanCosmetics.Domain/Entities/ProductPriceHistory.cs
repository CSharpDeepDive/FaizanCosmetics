using FaizanCosmetics.Domain.Common;
using FaizanCosmetics.Domain.Enums;

namespace FaizanCosmetics.Domain.Entities;

/// <summary>Immutable audit record of every price change on a Product. Never updated or deleted.</summary>
public class ProductPriceHistory : BaseEntity
{
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public decimal OldPurchasePrice { get; set; }
    public decimal NewPurchasePrice { get; set; }
    public decimal OldSellingPrice { get; set; }
    public decimal NewSellingPrice { get; set; }
    public decimal OldWholesalePrice { get; set; }
    public decimal NewWholesalePrice { get; set; }

    public int ChangedByUserId { get; set; }
    public User ChangedByUser { get; set; } = null!;
    public DateTime ChangedDate { get; set; } = DateTime.UtcNow;
    public PriceChangeReason Reason { get; set; }
    public string? Notes { get; set; }
}
