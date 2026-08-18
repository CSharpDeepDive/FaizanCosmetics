using FaizanCosmetics.Domain.Common;

namespace FaizanCosmetics.Domain.Entities;

public class SalesReturnItem : BaseEntity
{
    public int SalesReturnId { get; set; }
    public SalesReturn SalesReturn { get; set; } = null!;

    public int SalesInvoiceItemId { get; set; }
    public SalesInvoiceItem SalesInvoiceItem { get; set; } = null!;

    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
