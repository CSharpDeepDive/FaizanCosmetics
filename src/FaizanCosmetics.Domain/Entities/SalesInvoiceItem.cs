using FaizanCosmetics.Domain.Common;

namespace FaizanCosmetics.Domain.Entities;

/// <summary>
/// A line on a SalesInvoice. ProductNameSnapshot/BarcodeSnapshot/UnitPrice/tax/discount values are
/// captured at posting time and never recalculated from the live Product afterward — this is what
/// keeps historical invoices immutable when product data changes later.
/// </summary>
public class SalesInvoiceItem : BaseEntity
{
    public int SalesInvoiceId { get; set; }
    public SalesInvoice SalesInvoice { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public string ProductNameSnapshot { get; set; } = string.Empty;
    public string BarcodeSnapshot { get; set; } = string.Empty;

    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    /// <summary>Unit cost of goods sold at the time of sale — used for historically accurate profit reporting.</summary>
    public decimal UnitCostSnapshot { get; set; }

    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }

    public int? BatchId { get; set; }
    public ProductBatch? Batch { get; set; }

    /// <summary>Quantity already returned via SalesReturnItem rows against this line, kept in sync so remaining-returnable qty can be validated cheaply.</summary>
    public decimal QuantityReturned { get; set; }
}
