using FaizanCosmetics.Domain.Entities;

namespace FaizanCosmetics.Application.Interfaces;

public interface IClientPaymentRepository
{
    Task<(List<ClientPayment> Items, int TotalCount)> GetByClientAsync(int clientId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    void Add(ClientPayment payment);
}
