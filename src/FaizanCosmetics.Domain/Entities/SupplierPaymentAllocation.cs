using FaizanCosmetics.Domain.Common;

namespace FaizanCosmetics.Domain.Entities;

public class SupplierPaymentAllocation : BaseEntity
{
    public int SupplierPaymentId { get; set; }
    public SupplierPayment SupplierPayment { get; set; } = null!;

    public int PurchaseInvoiceId { get; set; }
    public PurchaseInvoice PurchaseInvoice { get; set; } = null!;

    public decimal AllocatedAmount { get; set; }
}
