using FaizanCosmetics.Domain.Entities;

namespace FaizanCosmetics.Application.Interfaces;

public interface IClientRepository
{
    Task<Client?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Client> GetWalkInCustomerAsync(CancellationToken cancellationToken = default);
    Task<(List<Client> Items, int TotalCount)> SearchAsync(string? searchText, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<bool> HasTransactionsAsync(int clientId, CancellationToken cancellationToken = default);
    Task<string> GenerateNextClientCodeAsync(CancellationToken cancellationToken = default);

    void Add(Client client);
    void Update(Client client);
}
