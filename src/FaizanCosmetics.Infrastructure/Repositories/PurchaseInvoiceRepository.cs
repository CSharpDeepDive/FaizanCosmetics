using FaizanCosmetics.Application.Interfaces;
using FaizanCosmetics.Domain.Entities;
using FaizanCosmetics.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FaizanCosmetics.Infrastructure.Repositories;

public class PurchaseInvoiceRepository : IPurchaseInvoiceRepository
{
    private readonly ApplicationDbContext _context;
    public PurchaseInvoiceRepository(ApplicationDbContext context) => _context = context;

    public Task<PurchaseInvoice?> GetByIdWithItemsAsync(int id, CancellationToken cancellationToken = default) =>
        _context.PurchaseInvoices
            .Include(i => i.Items).ThenInclude(it => it.Product)
            .Include(i => i.Supplier)
            .Include(i => i.CreatedByUser)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public async Task<string> GenerateNextInvoiceNumberAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var year = DateTime.UtcNow.Year;
        var yearPrefix = $"{prefix}-{year}-";

        var lastNumber = await _context.PurchaseInvoices
            .Where(i => i.InvoiceNumber.StartsWith(yearPrefix))
            .OrderByDescending(i => i.InvoiceNumber)
            .Select(i => i.InvoiceNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var nextSequence = 1;
        if (lastNumber != null)
        {
            var suffix = lastNumber[yearPrefix.Length..];
            if (int.TryParse(suffix, out var parsed)) nextSequence = parsed + 1;
        }

        return $"{yearPrefix}{nextSequence:D6}";
    }

    public async Task<(List<PurchaseInvoice> Items, int TotalCount)> SearchAsync(string? invoiceNumber, int? supplierId, DateTime? fromDate, DateTime? toDate, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.PurchaseInvoices.AsNoTracking().Include(i => i.Supplier).AsQueryable();

        if (!string.IsNullOrWhiteSpace(invoiceNumber)) query = query.Where(i => i.InvoiceNumber.Contains(invoiceNumber));
        if (supplierId.HasValue) query = query.Where(i => i.SupplierId == supplierId.Value);
        if (fromDate.HasValue) query = query.Where(i => i.InvoiceDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(i => i.InvoiceDate <= toDate.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(i => i.InvoiceDate)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public void Add(PurchaseInvoice invoice) => _context.PurchaseInvoices.Add(invoice);
    public void Update(PurchaseInvoice invoice) => _context.PurchaseInvoices.Update(invoice);
}
