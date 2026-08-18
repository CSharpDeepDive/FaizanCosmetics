using FaizanCosmetics.Application.Interfaces;
using FaizanCosmetics.Domain.Entities;
using FaizanCosmetics.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FaizanCosmetics.Infrastructure.Repositories;

public class ClientRepository : IClientRepository
{
    private readonly ApplicationDbContext _context;
    public ClientRepository(ApplicationDbContext context) => _context = context;

    public Task<Client?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Clients.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<Client> GetWalkInCustomerAsync(CancellationToken cancellationToken = default)
    {
        var client = await _context.Clients.FirstOrDefaultAsync(c => c.IsWalkInCustomer, cancellationToken);
        return client ?? throw new InvalidOperationException(
            "The Walk-in Customer record is missing. It should have been created by database seeding; re-run migrations/seeding to restore it.");
    }

    public async Task<(List<Client> Items, int TotalCount)> SearchAsync(string? searchText, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Clients.AsNoTracking().Where(c => c.IsActive).AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var text = searchText.Trim();
            query = query.Where(c => c.Name.Contains(text) || (c.Phone != null && c.Phone.Contains(text)) || c.ClientCode.Contains(text));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(c => c.Name).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<bool> HasTransactionsAsync(int clientId, CancellationToken cancellationToken = default) =>
        _context.SalesInvoices.AnyAsync(i => i.ClientId == clientId, cancellationToken);

    public async Task<string> GenerateNextClientCodeAsync(CancellationToken cancellationToken = default)
    {
        var count = await _context.Clients.CountAsync(cancellationToken);
        return $"CL-{(count + 1):D6}";
    }

    public void Add(Client client) => _context.Clients.Add(client);
    public void Update(Client client) => _context.Clients.Update(client);
}
