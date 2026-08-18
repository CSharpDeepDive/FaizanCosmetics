using FaizanCosmetics.Application.Interfaces;
using FaizanCosmetics.Domain.Entities;
using FaizanCosmetics.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FaizanCosmetics.Infrastructure.Repositories;

public class ClientLedgerRepository : IClientLedgerRepository
{
    private readonly ApplicationDbContext _context;
    public ClientLedgerRepository(ApplicationDbContext context) => _context = context;

    public async Task<decimal> GetBalanceAsync(int clientId, CancellationToken cancellationToken = default)
    {
        var entries = _context.ClientLedgerEntries.AsNoTracking().Where(l => l.ClientId == clientId);
        var debit = await entries.SumAsync(l => (decimal?)l.Debit, cancellationToken) ?? 0m;
        var credit = await entries.SumAsync(l => (decimal?)l.Credit, cancellationToken) ?? 0m;
        return debit - credit;
    }

    public async Task<Dictionary<int, decimal>> GetBalancesAsync(IEnumerable<int> clientIds, CancellationToken cancellationToken = default)
    {
        var ids = clientIds.ToList();
        if (ids.Count == 0) return new Dictionary<int, decimal>();

        // GroupBy + Sum translates cleanly to SQL (unlike GroupBy + OrderBy().First(), which EF
        // Core generally cannot translate) — computing the balance as Sum(Debit) - Sum(Credit)
        // rather than reading the last entry's stored running-Balance column sidesteps that
        // limitation entirely while remaining mathematically identical, since every entry's
        // Debit/Credit already reflects its own delta.
        var balances = await _context.ClientLedgerEntries.AsNoTracking()
            .Where(l => ids.Contains(l.ClientId))
            .GroupBy(l => l.ClientId)
            .Select(g => new { ClientId = g.Key, Balance = g.Sum(x => x.Debit) - g.Sum(x => x.Credit) })
            .ToListAsync(cancellationToken);

        var result = ids.ToDictionary(id => id, _ => 0m);
        foreach (var b in balances) result[b.ClientId] = b.Balance;
        return result;
    }

    public async Task<decimal> GetBalanceAsOfAsync(int clientId, DateTime asOfDate, CancellationToken cancellationToken = default)
    {
        var entries = _context.ClientLedgerEntries.AsNoTracking().Where(l => l.ClientId == clientId && l.EntryDate < asOfDate);
        var debit = await entries.SumAsync(l => (decimal?)l.Debit, cancellationToken) ?? 0m;
        var credit = await entries.SumAsync(l => (decimal?)l.Credit, cancellationToken) ?? 0m;
        return debit - credit;
    }

    public Task<List<ClientLedgerEntry>> GetStatementAsync(int clientId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        var query = _context.ClientLedgerEntries.AsNoTracking().Include(l => l.User).Where(l => l.ClientId == clientId);
        if (fromDate.HasValue) query = query.Where(l => l.EntryDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(l => l.EntryDate <= toDate.Value);
        return query.OrderBy(l => l.EntryDate).ThenBy(l => l.Id).ToListAsync(cancellationToken);
    }

    public void Add(ClientLedgerEntry entry) => _context.ClientLedgerEntries.Add(entry);
}
