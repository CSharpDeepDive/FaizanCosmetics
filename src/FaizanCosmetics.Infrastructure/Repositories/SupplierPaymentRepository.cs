using FaizanCosmetics.Application.Interfaces;
using FaizanCosmetics.Domain.Entities;
using FaizanCosmetics.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FaizanCosmetics.Infrastructure.Repositories;

public class SupplierPaymentRepository : ISupplierPaymentRepository
{
    private readonly ApplicationDbContext _context;
    public SupplierPaymentRepository(ApplicationDbContext context) => _context = context;

    public async Task<(List<SupplierPayment> Items, int TotalCount)> GetBySupplierAsync(int supplierId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.SupplierPayments.AsNoTracking().Where(p => p.SupplierId == supplierId);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(p => p.PaymentDate)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public void Add(SupplierPayment payment) => _context.SupplierPayments.Add(payment);
}
