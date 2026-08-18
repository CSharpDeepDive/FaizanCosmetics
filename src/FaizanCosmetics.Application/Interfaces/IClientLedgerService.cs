using FaizanCosmetics.Application.DTOs;
using FaizanCosmetics.Domain.Enums;

namespace FaizanCosmetics.Application.Interfaces;

/// <summary>
/// Posts entries to a client's Khata ledger. Every module that affects what a client owes
/// (client creation with an opening balance, receiving a payment, and — from Phase 5 onward —
/// credit sales and sales returns) posts through here, so the running-balance math lives in one
/// place. Does not manage its own transaction — callers needing atomicity with other writes
/// should wrap this inside IUnitOfWork.ExecuteInTransactionAsync alongside their other calls.
/// </summary>
public interface IClientLedgerService
{
    /// <summary>Posts one ledger entry and returns it with Balance correctly computed from the client's running total. Does not call SaveChangesAsync.</summary>
    Task<decimal> PostEntryAsync(
        int clientId,
        ClientLedgerEntryType entryType,
        ReferenceType referenceType,
        int referenceId,
        decimal debit,
        decimal credit,
        int userId,
        string? description = null,
        CancellationToken cancellationToken = default);

    Task<decimal> GetBalanceAsync(int clientId, CancellationToken cancellationToken = default);
    Task<ClientStatementDto> GetStatementAsync(int clientId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);
}
