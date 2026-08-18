using FaizanCosmetics.Application.Interfaces;
using FaizanCosmetics.Domain.Entities;
using FaizanCosmetics.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FaizanCosmetics.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _context;
    public ProductRepository(ApplicationDbContext context) => _context = context;

    public Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Products.Include(p => p.Category).Include(p => p.Supplier).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default) =>
        _context.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Barcode == barcode && p.IsActive, cancellationToken);

    public Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default) =>
        _context.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.SKU == sku && p.IsActive, cancellationToken);

    public Task<bool> BarcodeExistsAsync(string barcode, int? excludeProductId = null, CancellationToken cancellationToken = default) =>
        _context.Products.AnyAsync(p => p.Barcode == barcode && (excludeProductId == null || p.Id != excludeProductId), cancellationToken);

    public Task<bool> SkuExistsAsync(string sku, int? excludeProductId = null, CancellationToken cancellationToken = default) =>
        _context.Products.AnyAsync(p => p.SKU == sku && (excludeProductId == null || p.Id != excludeProductId), cancellationToken);

    public async Task<(List<Product> Items, int TotalCount)> SearchAsync(string? searchText, int? categoryId, bool activeOnly, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Products.AsNoTracking().Include(p => p.Category).AsQueryable();

        if (activeOnly) query = query.Where(p => p.IsActive);
        if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId.Value);
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var text = searchText.Trim();
            query = query.Where(p => p.Name.Contains(text) || p.Barcode.Contains(text) || p.SKU.Contains(text));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(p => p.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public Task<List<Product>> GetLowStockAsync(CancellationToken cancellationToken = default) =>
        _context.Products.AsNoTracking().Include(p => p.Category)
            .Where(p => p.IsActive && p.CurrentStock > 0 && p.CurrentStock <= p.MinimumStockLevel)
            .OrderBy(p => p.CurrentStock)
            .ToListAsync(cancellationToken);

    public Task<List<Product>> GetOutOfStockAsync(CancellationToken cancellationToken = default) =>
        _context.Products.AsNoTracking().Include(p => p.Category)
            .Where(p => p.IsActive && p.CurrentStock <= 0)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

    public Task<List<ProductPriceHistory>> GetPriceHistoryAsync(int productId, CancellationToken cancellationToken = default) =>
        _context.ProductPriceHistories.AsNoTracking()
            .Include(h => h.ChangedByUser)
            .Where(h => h.ProductId == productId)
            .OrderByDescending(h => h.ChangedDate)
            .ToListAsync(cancellationToken);

    public void Add(Product product) => _context.Products.Add(product);
    public void Update(Product product) => _context.Products.Update(product);
}
