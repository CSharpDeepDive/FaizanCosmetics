using FaizanCosmetics.Domain.Common;

namespace FaizanCosmetics.Domain.Entities;

public class PurchaseInvoiceItem : BaseEntity
{
    public int PurchaseInvoiceId { get; set; }
    public PurchaseInvoice PurchaseInvoice { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public string ProductNameSnapshot { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }

    /// <summary>Set when this receipt creates/replenishes a batch (only for products with HasExpiry).</summary>
    public int? BatchId { get; set; }
    public ProductBatch? Batch { get; set; }

    public decimal QuantityReturned { get; set; }
}
