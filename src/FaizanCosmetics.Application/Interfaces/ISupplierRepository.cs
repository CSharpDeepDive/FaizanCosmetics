using FaizanCosmetics.Domain.Entities;

namespace FaizanCosmetics.Application.Interfaces;

public interface ISupplierRepository
{
    Task<Supplier?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(List<Supplier> Items, int TotalCount)> SearchAsync(string? searchText, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<bool> HasTransactionsAsync(int supplierId, CancellationToken cancellationToken = default);

    void Add(Supplier supplier);
    void Update(Supplier supplier);
}
