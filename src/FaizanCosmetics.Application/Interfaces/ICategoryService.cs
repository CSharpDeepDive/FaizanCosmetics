using FaizanCosmetics.Application.DTOs;

namespace FaizanCosmetics.Application.Interfaces;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllAsync(bool activeOnly, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(string name, string? description, CancellationToken cancellationToken = default);
    Task UpdateAsync(int id, string name, string? description, CancellationToken cancellationToken = default);
    Task DeactivateAsync(int id, CancellationToken cancellationToken = default);
}
