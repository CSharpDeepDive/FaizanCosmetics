using FaizanCosmetics.Application.Interfaces;
using FaizanCosmetics.Domain.Entities;
using FaizanCosmetics.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FaizanCosmetics.Infrastructure.Repositories;

public class ClientPaymentRepository : IClientPaymentRepository
{
    private readonly ApplicationDbContext _context;
    public ClientPaymentRepository(ApplicationDbContext context) => _context = context;

    public async Task<(List<ClientPayment> Items, int TotalCount)> GetByClientAsync(int clientId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.ClientPayments.AsNoTracking().Where(p => p.ClientId == clientId);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(p => p.PaymentDate)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public void Add(ClientPayment payment) => _context.ClientPayments.Add(payment);
}
