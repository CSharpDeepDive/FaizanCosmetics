using FaizanCosmetics.Domain.Common;

namespace FaizanCosmetics.Domain.Entities;

public class PurchaseReturnItem : BaseEntity
{
    public int PurchaseReturnId { get; set; }
    public PurchaseReturn PurchaseReturn { get; set; } = null!;

    public int PurchaseInvoiceItemId { get; set; }
    public PurchaseInvoiceItem PurchaseInvoiceItem { get; set; } = null!;

    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineTotal { get; set; }
}
