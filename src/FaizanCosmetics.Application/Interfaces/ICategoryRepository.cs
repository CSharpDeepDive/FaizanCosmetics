using FaizanCosmetics.Domain.Entities;

namespace FaizanCosmetics.Application.Interfaces;

public interface ICategoryRepository
{
    Task<List<Category>> GetAllAsync(bool activeOnly, CancellationToken cancellationToken = default);
    Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(string name, int? excludeCategoryId = null, CancellationToken cancellationToken = default);
    Task<int> GetProductCountAsync(int categoryId, CancellationToken cancellationToken = default);
    void Add(Category category);
    void Update(Category category);
}
