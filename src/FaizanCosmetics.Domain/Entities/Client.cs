using FaizanCosmetics.Domain.Common;
using FaizanCosmetics.Domain.Enums;

namespace FaizanCosmetics.Domain.Entities;

public class Client : SoftDeletableEntity
{
    public string ClientCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }
    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
    public ClientType ClientType { get; set; } = ClientType.Retail;
    public decimal CreditLimit { get; set; }
    public decimal OpeningBalance { get; set; }

    /// <summary>True for the single system-managed "Walk-in Customer" record. Cannot be deactivated or deleted.</summary>
    public bool IsWalkInCustomer { get; set; }

    public ICollection<SalesInvoice> SalesInvoices { get; set; } = new List<SalesInvoice>();
    public ICollection<ClientLedgerEntry> LedgerEntries { get; set; } = new List<ClientLedgerEntry>();
}
