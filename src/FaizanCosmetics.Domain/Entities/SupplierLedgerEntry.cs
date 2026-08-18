using FaizanCosmetics.Domain.Common;
using FaizanCosmetics.Domain.Enums;

namespace FaizanCosmetics.Domain.Entities;

public class SupplierLedgerEntry : BaseEntity
{
    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;

    public DateTime EntryDate { get; set; } = DateTime.UtcNow;
    public SupplierLedgerEntryType EntryType { get; set; }
    public ReferenceType ReferenceType { get; set; }
    public int ReferenceId { get; set; }

    /// <summary>Debit reduces what we owe the supplier (e.g. a payment we make); Credit increases it (e.g. a purchase).</summary>
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }

    public string? Description { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;
}
