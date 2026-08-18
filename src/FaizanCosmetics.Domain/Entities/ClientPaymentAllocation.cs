using FaizanCosmetics.Domain.Common;

namespace FaizanCosmetics.Domain.Entities;

/// <summary>How much of a ClientPayment was applied against a specific SalesInvoice. A payment with no invoice allocations is a general advance/on-account credit.</summary>
public class ClientPaymentAllocation : BaseEntity
{
    public int ClientPaymentId { get; set; }
    public ClientPayment ClientPayment { get; set; } = null!;

    public int SalesInvoiceId { get; set; }
    public SalesInvoice SalesInvoice { get; set; } = null!;

    public decimal AllocatedAmount { get; set; }
}
