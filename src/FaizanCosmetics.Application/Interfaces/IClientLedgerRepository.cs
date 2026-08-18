using FaizanCosmetics.Domain.Entities;

namespace FaizanCosmetics.Application.Interfaces;

public interface IClientLedgerRepository
{
    /// <summary>Outstanding balance for one client: Sum(Debit) - Sum(Credit) across all their ledger entries. Positive means the client owes money; negative means they have credit/advance on account. Zero (not an exception) for a client with no entries yet.</summary>
    Task<decimal> GetBalanceAsync(int clientId, CancellationToken cancellationToken = default);

    /// <summary>Balance for many clients in one query — used by the client list screen to avoid an N+1 query per row.</summary>
    Task<Dictionary<int, decimal>> GetBalancesAsync(IEnumerable<int> clientIds, CancellationToken cancellationToken = default);

    /// <summary>Sum of all entries strictly before <paramref name="asOfDate"/> — the "opening balance" for a statement filtered from that date.</summary>
    Task<decimal> GetBalanceAsOfAsync(int clientId, DateTime asOfDate, CancellationToken cancellationToken = default);

    Task<List<ClientLedgerEntry>> GetStatementAsync(int clientId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);

    void Add(ClientLedgerEntry entry);
}
