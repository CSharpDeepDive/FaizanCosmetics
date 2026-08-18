using FaizanCosmetics.Domain.Common;

namespace FaizanCosmetics.Domain.Entities;

public class Supplier : SoftDeletableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }
    public string? ContactPerson { get; set; }
    public decimal OpeningBalance { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<PurchaseInvoice> PurchaseInvoices { get; set; } = new List<PurchaseInvoice>();
    public ICollection<SupplierLedgerEntry> LedgerEntries { get; set; } = new List<SupplierLedgerEntry>();
}
