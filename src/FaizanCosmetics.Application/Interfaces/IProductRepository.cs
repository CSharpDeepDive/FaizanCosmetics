using FaizanCosmetics.Domain.Entities;

namespace FaizanCosmetics.Application.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
    Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default);
    Task<bool> BarcodeExistsAsync(string barcode, int? excludeProductId = null, CancellationToken cancellationToken = default);
    Task<bool> SkuExistsAsync(string sku, int? excludeProductId = null, CancellationToken cancellationToken = default);

    /// <summary>Server-side paged, filtered, AsNoTracking search for the product list screen. Never loads the full product table.</summary>
    Task<(List<Product> Items, int TotalCount)> SearchAsync(string? searchText, int? categoryId, bool activeOnly, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<List<Product>> GetLowStockAsync(CancellationToken cancellationToken = default);
    Task<List<Product>> GetOutOfStockAsync(CancellationToken cancellationToken = default);
    Task<List<ProductPriceHistory>> GetPriceHistoryAsync(int productId, CancellationToken cancellationToken = default);

    void Add(Product product);
    void Update(Product product);
}
