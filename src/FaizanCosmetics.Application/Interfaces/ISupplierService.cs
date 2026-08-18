using FaizanCosmetics.Application.DTOs;

namespace FaizanCosmetics.Application.Interfaces;

public interface ISupplierService
{
    Task<(List<SupplierListItemDto> Items, int TotalCount)> SearchAsync(string? searchText, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<SupplierDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(CreateSupplierDto dto, int currentUserId, CancellationToken cancellationToken = default);
    Task UpdateAsync(UpdateSupplierDto dto, int currentUserId, CancellationToken cancellationToken = default);
    Task DeactivateAsync(int id, int currentUserId, CancellationToken cancellationToken = default);
    Task ReactivateAsync(int id, int currentUserId, CancellationToken cancellationToken = default);
}
