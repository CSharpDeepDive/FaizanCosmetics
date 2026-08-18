using FaizanCosmetics.Application.Interfaces;
using FaizanCosmetics.Domain.Entities;
using FaizanCosmetics.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FaizanCosmetics.Infrastructure.Repositories;

public class SupplierRepository : ISupplierRepository
{
    private readonly ApplicationDbContext _context;
    public SupplierRepository(ApplicationDbContext context) => _context = context;

    public Task<Supplier?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Suppliers.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<(List<Supplier> Items, int TotalCount)> SearchAsync(string? searchText, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Suppliers.AsNoTracking().Where(s => s.IsActive).AsQueryable();
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var text = searchText.Trim();
            query = query.Where(s => s.Name.Contains(text) || (s.Phone != null && s.Phone.Contains(text)));
        }
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(s => s.Name).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<bool> HasTransactionsAsync(int supplierId, CancellationToken cancellationToken = default) =>
        _context.PurchaseInvoices.AnyAsync(p => p.SupplierId == supplierId, cancellationToken);

    public void Add(Supplier supplier) => _context.Suppliers.Add(supplier);
    public void Update(Supplier supplier) => _context.Suppliers.Update(supplier);
}
