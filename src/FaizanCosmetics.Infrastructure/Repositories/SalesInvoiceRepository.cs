using FaizanCosmetics.Application.Interfaces;
using FaizanCosmetics.Domain.Entities;
using FaizanCosmetics.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FaizanCosmetics.Infrastructure.Repositories;

public class SalesInvoiceRepository : ISalesInvoiceRepository
{
    private readonly ApplicationDbContext _context;
    public SalesInvoiceRepository(ApplicationDbContext context) => _context = context;

    public Task<SalesInvoice?> GetByIdWithItemsAsync(int id, CancellationToken cancellationToken = default) =>
        _context.SalesInvoices
            .Include(i => i.Items).ThenInclude(it => it.Product)
            .Include(i => i.Client)
            .Include(i => i.CreatedByUser)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public Task<SalesInvoice?> GetByInvoiceNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default) =>
        _context.SalesInvoices.Include(i => i.Items).FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber, cancellationToken);

    public async Task<string> GenerateNextInvoiceNumberAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var year = DateTime.UtcNow.Year;
        var yearPrefix = $"{prefix}-{year}-";

        // MAX on the numeric suffix (not COUNT) so numbers stay monotonically increasing even
        // after cancellations, avoiding a duplicate-number race under concurrent posting.
        var lastNumber = await _context.SalesInvoices
            .Where(i => i.InvoiceNumber.StartsWith(yearPrefix))
            .OrderByDescending(i => i.InvoiceNumber)
            .Select(i => i.InvoiceNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var nextSequence = 1;
        if (lastNumber != null)
        {
            var suffix = lastNumber[yearPrefix.Length..];
            if (int.TryParse(suffix, out var parsed))
            {
                nextSequence = parsed + 1;
            }
        }

        return $"{yearPrefix}{nextSequence:D6}";
    }

    public async Task<(List<SalesInvoice> Items, int TotalCount)> SearchAsync(string? invoiceNumber, int? clientId, DateTime? fromDate, DateTime? toDate, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.SalesInvoices.AsNoTracking().Include(i => i.Client).AsQueryable();

        if (!string.IsNullOrWhiteSpace(invoiceNumber)) query = query.Where(i => i.InvoiceNumber.Contains(invoiceNumber));
        if (clientId.HasValue) query = query.Where(i => i.ClientId == clientId.Value);
        if (fromDate.HasValue) query = query.Where(i => i.InvoiceDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(i => i.InvoiceDate <= toDate.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(i => i.InvoiceDate)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public void Add(SalesInvoice invoice) => _context.SalesInvoices.Add(invoice);
    public void Update(SalesInvoice invoice) => _context.SalesInvoices.Update(invoice);
}
