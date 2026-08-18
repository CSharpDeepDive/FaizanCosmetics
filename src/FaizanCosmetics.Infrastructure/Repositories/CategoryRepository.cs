using FaizanCosmetics.Application.Interfaces;
using FaizanCosmetics.Domain.Entities;
using FaizanCosmetics.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FaizanCosmetics.Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly ApplicationDbContext _context;
    public CategoryRepository(ApplicationDbContext context) => _context = context;

    public Task<List<Category>> GetAllAsync(bool activeOnly, CancellationToken cancellationToken = default) =>
        _context.Categories.AsNoTracking().Where(c => !activeOnly || c.IsActive).OrderBy(c => c.Name).ToListAsync(cancellationToken);

    public Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<bool> NameExistsAsync(string name, int? excludeCategoryId = null, CancellationToken cancellationToken = default) =>
        _context.Categories.AnyAsync(c => c.Name == name && (excludeCategoryId == null || c.Id != excludeCategoryId), cancellationToken);

    public Task<int> GetProductCountAsync(int categoryId, CancellationToken cancellationToken = default) =>
        _context.Products.CountAsync(p => p.CategoryId == categoryId, cancellationToken);

    public void Add(Category category) => _context.Categories.Add(category);
    public void Update(Category category) => _context.Categories.Update(category);
}
