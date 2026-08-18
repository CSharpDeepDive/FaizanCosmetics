using FaizanCosmetics.Application.Interfaces;
using FaizanCosmetics.Domain.Entities;
using FaizanCosmetics.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FaizanCosmetics.Infrastructure.Repositories;

public class SupplierLedgerRepository : ISupplierLedgerRepository
{
    private readonly ApplicationDbContext _context;
    public SupplierLedgerRepository(ApplicationDbContext context) => _context = context;

    public async Task<decimal> GetBalanceAsync(int supplierId, CancellationToken cancellationToken = default)
    {
        var entries = _context.SupplierLedgerEntries.AsNoTracking().Where(l => l.SupplierId == supplierId);
        var credit = await entries.SumAsync(l => (decimal?)l.Credit, cancellationToken) ?? 0m;
        var debit = await entries.SumAsync(l => (decimal?)l.Debit, cancellationToken) ?? 0m;
        return credit - debit; // Credit increases what we owe; Debit (payments) decreases it
    }

    public async Task<Dictionary<int, decimal>> GetBalancesAsync(IEnumerable<int> supplierIds, CancellationToken cancellationToken = default)
    {
        var ids = supplierIds.ToList();
        if (ids.Count == 0) return new Dictionary<int, decimal>();

        var balances = await _context.SupplierLedgerEntries.AsNoTracking()
            .Where(l => ids.Contains(l.SupplierId))
            .GroupBy(l => l.SupplierId)
            .Select(g => new { SupplierId = g.Key, Balance = g.Sum(x => x.Credit) - g.Sum(x => x.Debit) })
            .ToListAsync(cancellationToken);

        var result = ids.ToDictionary(id => id, _ => 0m);
        foreach (var b in balances) result[b.SupplierId] = b.Balance;
        return result;
    }

    public async Task<decimal> GetBalanceAsOfAsync(int supplierId, DateTime asOfDate, CancellationToken cancellationToken = default)
    {
        var entries = _context.SupplierLedgerEntries.AsNoTracking().Where(l => l.SupplierId == supplierId && l.EntryDate < asOfDate);
        var credit = await entries.SumAsync(l => (decimal?)l.Credit, cancellationToken) ?? 0m;
        var debit = await entries.SumAsync(l => (decimal?)l.Debit, cancellationToken) ?? 0m;
        return credit - debit;
    }

    public Task<List<SupplierLedgerEntry>> GetStatementAsync(int supplierId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        var query = _context.SupplierLedgerEntries.AsNoTracking().Include(l => l.User).Where(l => l.SupplierId == supplierId);
        if (fromDate.HasValue) query = query.Where(l => l.EntryDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(l => l.EntryDate <= toDate.Value);
        return query.OrderBy(l => l.EntryDate).ThenBy(l => l.Id).ToListAsync(cancellationToken);
    }

    public void Add(SupplierLedgerEntry entry) => _context.SupplierLedgerEntries.Add(entry);
}
