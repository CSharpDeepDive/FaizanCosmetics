using FaizanCosmetics.Domain.Common;
using FaizanCosmetics.Domain.Enums;

namespace FaizanCosmetics.Domain.Entities;

public class SalesInvoice : BaseEntity
{
    /// <summary>Unique, auto-generated. Format: INV-{year}-{6-digit sequence}, e.g. INV-2026-000001.</summary>
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;

    /// <summary>Null only if ever allowed for anonymous walk-in without even the default walk-in Client row; in practice always set to the Walk-in Customer or a real Client.</summary>
    public int? ClientId { get; set; }
    public Client? Client { get; set; }

    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DueAmount { get; set; }

    public PaymentStatus PaymentStatus { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public string? Notes { get; set; }

    /// <summary>Set when Status transitions to Cancelled. A cancelled invoice's stock/ledger effects are reversed via offsetting InventoryTransaction/ClientLedgerEntry rows, never by deleting history.</summary>
    public string? CancellationReason { get; set; }
    public int? CancelledByUserId { get; set; }
    public DateTime? CancelledDate { get; set; }

    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public ICollection<SalesInvoiceItem> Items { get; set; } = new List<SalesInvoiceItem>();
    public ICollection<SalesReturn> Returns { get; set; } = new List<SalesReturn>();
    public ICollection<ClientPaymentAllocation> PaymentAllocations { get; set; } = new List<ClientPaymentAllocation>();
}
