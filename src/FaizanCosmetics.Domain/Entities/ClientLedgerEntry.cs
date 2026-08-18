using FaizanCosmetics.Domain.Common;
using FaizanCosmetics.Domain.Enums;

namespace FaizanCosmetics.Domain.Entities;

/// <summary>
/// One immutable row per financial event affecting a client's Khata balance.
/// Balance is the running balance immediately after this entry (Debit increases what the
/// client owes; Credit decreases it). Never edit or delete a posted entry — post a reversing
/// Adjustment entry instead.
/// </summary>
public class ClientLedgerEntry : BaseEntity
{
    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;

    public DateTime EntryDate { get; set; } = DateTime.UtcNow;
    public ClientLedgerEntryType EntryType { get; set; }
    public ReferenceType ReferenceType { get; set; }
    public int ReferenceId { get; set; }

    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }

    public string? Description { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;
}
