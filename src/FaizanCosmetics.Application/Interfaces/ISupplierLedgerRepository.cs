using FaizanCosmetics.Domain.Entities;

namespace FaizanCosmetics.Application.Interfaces;

/// <summary>Mirrors IClientLedgerRepository exactly, including the Sum(Debit)-Sum(Credit) balance
/// pattern (not the last row's stored running-Balance column) — see that interface's comments for
/// why. Sign convention is opposite to the client ledger: Credit increases what WE owe the
/// supplier (e.g. a purchase); Debit decreases it (e.g. a payment we make) — see
/// SupplierLedgerEntry's own doc comment on the entity.</summary>
public interface ISupplierLedgerRepository
{
    Task<decimal> GetBalanceAsync(int supplierId, CancellationToken cancellationToken = default);
    Task<Dictionary<int, decimal>> GetBalancesAsync(IEnumerable<int> supplierIds, CancellationToken cancellationToken = default);
    Task<decimal> GetBalanceAsOfAsync(int supplierId, DateTime asOfDate, CancellationToken cancellationToken = default);
    Task<List<SupplierLedgerEntry>> GetStatementAsync(int supplierId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);
    void Add(SupplierLedgerEntry entry);
}
